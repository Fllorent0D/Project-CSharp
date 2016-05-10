﻿using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace ProjectSchool
{
    public partial class Form1 : Form
    {
        private BindingList<Transaction> achats;
        private ListCategorie listeCategorie;

        public Form1()
        {
            InitializeComponent();

            achats = new BindingList<Transaction>();
            listeCategorie = new ListCategorie();
            listeCategorie.TagTree = achatTreeView;

            BindingSource source = new BindingSource(achats, null);
            achatGridView.DataSource = source;

            ContextMenu menuTree = new ContextMenu();
            MenuItem item = new MenuItem("Ajouter une catégorie", new EventHandler(this.addItem));
            menuTree.MenuItems.Add(item);
            achatTreeView.ContextMenu = menuTree;

        }
        private void addItem(object sender, EventArgs e)
        {
          
            achatTreeView.BeginUpdate();

            Categorie newCate = new Categorie("Catégorie");
            TreeNode test = new TreeNode(newCate.Nomcategorie);
            test.Tag = newCate;

            ContextMenuStrip menuTree = new ContextMenuStrip();
            menuTree.Opening += new CancelEventHandler(this.openContextItem);
            ToolStripMenuItem itemAjouter = new ToolStripMenuItem("Ajouter une sous-catégorie", null, new EventHandler(this.addItem));
            ToolStripMenuItem itemRenommer = new ToolStripMenuItem("Renommer", null, new EventHandler(this.renameItem));
            ToolStripMenuItem itemSupprimer = new ToolStripMenuItem("Supprimer", null, new EventHandler(this.deleteItem));

            menuTree.Items.Add(itemAjouter);
            menuTree.Items.Add(itemRenommer);
            menuTree.Items.Add(itemSupprimer);


            test.ContextMenuStrip = menuTree;
          
            if (sender is ToolStripMenuItem)
            {
                
                ToolStripMenuItem item = (ToolStripMenuItem)sender;
                ContextMenuStrip menu = item.Owner as ContextMenuStrip;
                TreeNode usedNode = achatTreeView.GetNodeAt(achatTreeView.PointToClient(new Point(menu.Left, menu.Top)));
                if (usedNode != null)
                {
                    
                    (usedNode.Tag as Categorie).Children.Add(test.Tag as Categorie);
                    usedNode.Nodes.Add(test);
                   
                }
                usedNode.Expand();
            }
            else
            {
                achatTreeView.Nodes.Add(test);              
                listeCategorie.List.Add(newCate);

            }
            test.BeginEdit();
            achatTreeView.EndUpdate();
        }

        private void openContextItem(object sender, CancelEventArgs e)
        {
            ContextMenuStrip menu = sender as ContextMenuStrip;
            TreeNode usedNode = achatTreeView.GetNodeAt(achatTreeView.PointToClient(new Point(menu.Left, menu.Top)));
            System.Console.WriteLine();
            ToolStripMenuItem itemcat = menu.Items[0] as ToolStripMenuItem;
            ToolStripMenuItem itemsup = menu.Items[2] as ToolStripMenuItem;
            itemsup.Enabled = true;

            itemcat.Enabled = true;
            if (usedNode != null)
            {
                foreach (object obj in (usedNode.Tag as Categorie).Children)
                {
                    itemsup.Enabled = false;

                    if (obj is Transaction)
                    {
                        itemcat.Enabled = false;
                    }
                }
               
            }
        }

        private void deleteItem(object sender, EventArgs e)
        {
            ToolStripMenuItem item = (ToolStripMenuItem)sender;
            ContextMenuStrip menu = item.Owner as ContextMenuStrip;
            TreeNode usedNode = achatTreeView.GetNodeAt(achatTreeView.PointToClient(new Point(menu.Left, menu.Top)));
            if (usedNode != null)
            {
                if (usedNode.Parent != null)
                    (usedNode.Parent.Tag as Categorie).Children.Remove(usedNode.Tag);
                else
                    achatTreeView.Nodes.Remove(usedNode);

                usedNode.Remove();
            }
        }
        private void renameItem(object sender, EventArgs e)
        {
            ToolStripMenuItem item = (ToolStripMenuItem)sender;
            ContextMenuStrip menu = item.Owner as ContextMenuStrip;
            TreeNode usedNode = achatTreeView.GetNodeAt(achatTreeView.PointToClient(new Point(menu.Left, menu.Top)));
            if (usedNode != null)
            {
                usedNode.BeginEdit();
            }

        }
        private void Form1_Load(object sender, EventArgs e)
        {

        }

      
        

        private void chargerDesDonnéesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Parser
            OpenFileDialog fichier = new OpenFileDialog();
            fichier.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer);

            if (fichier.ShowDialog() == DialogResult.OK)
            {
                TextFieldParser parser = new TextFieldParser(fichier.FileName);
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");
                while (!parser.EndOfData)
                {
                    string[] fields = parser.ReadFields();
                    foreach (string field in fields)
                    {
                        string[] rows = field.Split(default(char), ';');
                        string[] date = rows[2].Split(default(char), '-');

                        Transaction nouvMontant = new Transaction(rows[0], rows[1], new DateTime(Convert.ToInt32(date[2]), Convert.ToInt32(date[1]), Convert.ToInt32(date[0])), Convert.ToDouble(rows[3]));
                        System.Console.WriteLine(nouvMontant);
                        achats.Add(nouvMontant);
                    }
                }

            }
        }

        private void achatGridView_MouseDown(object sender, MouseEventArgs e)
        {
            //Clique droit sur tuple
            if (e.Button == MouseButtons.Right)
            {
                DataGridView.HitTestInfo info = achatGridView.HitTest(e.X, e.Y);
                if (info.RowIndex >= 0)
                {
                    Transaction view = (Transaction)achatGridView.Rows[info.RowIndex].DataBoundItem;
                    if (view != null)
                    {
                        achatGridView.DoDragDrop(view, DragDropEffects.Copy);
                    }

                }
            }
        }

        private void achatTreeView_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Copy;
            e.Effect = e.AllowedEffect;
        }

        private void achatTreeView_DragDrop(object sender, DragEventArgs e)
        {
            Point p = achatTreeView.PointToClient(new Point(e.X, e.Y));
            TreeNode nodeUsed = (TreeNode)achatTreeView.GetNodeAt(p);

            //Si c'est un Montant, vue de gauche
            if (e.Data.GetDataPresent(typeof(Transaction)))
            {
                Transaction montantPassed = (Transaction)e.Data.GetData(typeof(Transaction));

                //Si on le lache dans le vide :'(
                if (nodeUsed == null)
                {
                    System.Console.WriteLine("Vide");
                }
                else
                {
                    if (nodeUsed.Tag is Transaction)
                        nodeUsed = nodeUsed.Parent;

                    TreeNode newNode = new TreeNode(montantPassed.ToString());
                    newNode.Tag = montantPassed;

                    
                    foreach (object obj in (nodeUsed.Tag as Categorie).Children)
                    {
                        if (obj is Categorie)
                        {
                            MessageBox.Show("Vous ne pouvez pas placer une transaction a cote d'une categorie", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
                            return;
                        }
                    }
                    
                    

                    (nodeUsed.Tag as Categorie).Children.Add(montantPassed);
                    nodeUsed.Nodes.Add(newNode);
                    nodeUsed.Expand();
                }

            }
            else if (e.Data.GetDataPresent(typeof(TreeNode)))
            {
                TreeNode draggedNode = (TreeNode)e.Data.GetData(typeof(TreeNode));
                if (!draggedNode.Equals(nodeUsed) && !ContainsNode(draggedNode, nodeUsed))
                {

                    if (nodeUsed == null)
                    {
                        //Si on drop à la racine

                        if (draggedNode.Tag is Categorie)                                           //Une catégorie à la racine = ok
                        {

                            draggedNode.Remove();

                            if (draggedNode.Parent != null)
                            {
                                (draggedNode.Parent.Tag as Categorie).Children.Remove(draggedNode.Tag);

                            }

                            achatTreeView.Nodes.Add(draggedNode);
                            listeCategorie.List.Add(draggedNode.Tag as Categorie);
                        }
                    }
                    else
                    {
                        //Si on drop dans un node
                        if (nodeUsed.Tag is Transaction)                                                //Si c'est une transaction alors on ajoute dans le parent
                            nodeUsed = nodeUsed.Parent;

                        if (draggedNode.Tag is Transaction)                                              //on ne peut pas avoir de transaction a cote de categorie
                        {
                            foreach (object obj in (nodeUsed.Tag as Categorie).Children)
                            {
                                if (obj is Categorie)
                                {
                                    MessageBox.Show("Vous ne pouvez pas placer une transaction a cote d'une categorie", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
                                    return;
                                }
                            }
                        }
                        else if (draggedNode.Tag is Categorie)                                               //Idem mais inversément
                        {
                            foreach (object obj in (nodeUsed.Tag as Categorie).Children)
                            {
                                if (obj is Transaction)
                                {
                                    MessageBox.Show("Vous ne pouvez pas placer une categorie a cote de transactions", "Erreur", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
                                    return;
                                }
                            }
                        }
                        if (draggedNode.Parent != null)
                        {
                            if (nodeUsed == draggedNode.Parent)                                        //draggedNode n'a pas bougé
                                return;

                            (draggedNode.Parent.Tag as Categorie).Children.Remove(draggedNode.Tag);     //Retirer une catégorie qui n'est pas la racine
                        }
                        else
                            listeCategorie.List.Remove(draggedNode.Tag as Categorie);               //Retirer une catégorie qui est à la racine



                        (nodeUsed.Tag as Categorie).Children.Add(draggedNode.Tag);                  //Mettre à jour la liste d'enfant dans la catégorie

                        draggedNode.Remove();                                                       //Mettre à jour la treeview
                        nodeUsed.Nodes.Add(draggedNode);
                        nodeUsed.Expand();

                    }
                } 
            }
        }
        private bool ContainsNode(TreeNode node1, TreeNode node2)
        {

            if (node2 == null || node2.Parent == null)
                return false;
            if (node2.Parent.Equals(node1))
                return true;


            return ContainsNode(node1, node2.Parent);
        }
        private void sauvegarderDesDonnéesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listeCategorie.Save();
        }

        private void achatTreeView_DragOver(object sender, DragEventArgs e)
        {
            Point targetPoint = achatTreeView.PointToClient(new Point(e.X, e.Y));
            TreeNode usedNode = (TreeNode)achatTreeView.GetNodeAt(targetPoint);
            if (usedNode == null)
                return;

            if (usedNode.Tag is Categorie)
            {
                achatTreeView.SelectedNode = usedNode;
            }
            else if (usedNode.Tag is Transaction)
            {
                achatTreeView.SelectedNode = usedNode.Parent;
            }
        }

        private void achatTreeView_ItemDrag(object sender, ItemDragEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                DoDragDrop(e.Item, DragDropEffects.Move);
            }
        }

        private void chargerDesDonnéesDéjàImputéesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog fichier = new OpenFileDialog();
            fichier.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyComputer);

            if (fichier.ShowDialog() == DialogResult.OK)
            {
                XmlSerializer xmlFormat = new XmlSerializer(typeof(ListCategorie));
                using (Stream fStream = File.OpenRead(fichier.FileName))
                {
                    listeCategorie = (ListCategorie)xmlFormat.Deserialize(fStream);
                    listeCategorie.TagTree = achatTreeView;
                    listeCategorie.CreateTree();
                }
            }
        }



        private void achatTreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            System.Console.WriteLine(sender);
            if(e.Button == MouseButtons.Right)
            {
                ContextMenuStrip menu = new ContextMenuStrip();
                
            }
        }

        private void achatTreeView_AfterLabelEdit(object sender, NodeLabelEditEventArgs e)
        {
            
            if (e.Label == null || e.Label.Length <= 0)
            {
                e.CancelEdit = true;
                MessageBox.Show("La catégorie ne peut pas être vide",
                   "Nouvelle catégorie");
                e.Node.BeginEdit();
            }
            else
            {
                e.Node.EndEdit(false);
                (e.Node.Tag as Categorie).Nomcategorie = e.Label;
                
            }
        }
    }
}
