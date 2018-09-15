using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Data.Sql;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;


namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        private Operation _groupOper;
        private const string TechnicalGroupKey = "TechnicalGroup";
        private ModelContext _model;
        public Form1()
        {
            InitializeComponent();

            treeView1.BeforeExpand += treeView1_BeforeExpand;
            this.FormClosed += Form1_FormClosed;
            _model = new ModelContext();
            InitRootGroup();
            _groupOper = Operation.NONE;
            Show1(false);
            Show2(false);
            

        }
        private void treeView1_AfterExpand1(object sender, TreeViewEventArgs e)
        {
            var selectedNode = treeView1.SelectedNode;
            if (selectedNode == null)
            {
                MessageBox.Show("Вы не выбрали элемент");
                return;
            }
            var name = selectedNode.Name;
            if (name == null)
            {
                return;
            }
            var id = name.Split('|');
            var IdProp = Convert.ToInt64(id[0]);

            var propGroups = _model.EnGroup.Join(_model.EnProperty.Where(y => y.id_group == IdProp),
                   gr => gr.Id,
                   rel => rel.id_group,
                   (gr, rel) => new CustomGroup2()
                   {
                       Name = rel.Name,
                       Id = rel.Id
                   });
            if (propGroups == null)
            {
                return;
            }
            foreach (var pr in propGroups)
            {
                var Property = new TreeNode()
                {
                    Name = pr.Name,
                    Text = pr.Id + "|Property"
                };
                var childTechGroup = new TreeNode()
                {
                    Name = TechnicalGroupKey,
                    Text = TechnicalGroupKey
                };
                Property.Nodes.Add(childTechGroup);
                e.Node.FirstNode.Nodes.Add(Property);
            }
        }

        private void treeView1_BeforeExpand(object sender, TreeViewCancelEventArgs e)
        {
            e.Node.Nodes.Clear();
            if (e.Node == null) return;
            var techGroup = e.Node.Nodes.Find(TechnicalGroupKey, false).FirstOrDefault();
            if (techGroup != null)
                e.Node.Nodes.Remove(techGroup);

            if (e.Node.Name == null) return;
            var arrType = e.Node.Name.Split('|');
            if (arrType.Length < 2)
            {
                return;
            }

            var idGroup = Convert.ToInt64(arrType[0]);

            if (arrType[1] == "Group")
            {
                treeView1.BeginUpdate();
                var childGroups = _model.EnGroup.Join(
                    _model.EnRelation.Where(x => x.Id_parent == idGroup),
                    gr => gr.Id,
                    rel => rel.Id_child,
                    (gr, rel) => new CustomGroup()
                    {
                        Name = gr.Name,
                        Id = gr.Id
                    });
                var propGroups = _model.EnProperty.ToList();
                var Groups = _model.EnGroup.ToList();
                foreach (var child in childGroups)
                {

                    var childTreeNode = new TreeNode()
                    {
                        Text = child.Name,
                        Name = child.Id.ToString() + "|Group"
                    };
                    var childTechGroup = new TreeNode()
                    {
                        Name = TechnicalGroupKey,
                        Text = TechnicalGroupKey
                    };
                    childTreeNode.Nodes.Add(childTechGroup);
                    e.Node.Nodes.Add(childTreeNode);
                }
                foreach (var pr in propGroups)
                {

                    if (pr.id_group == idGroup)
                    {
                        var Property = new TreeNode()
                        {
                            Name = pr.Id + "|Property",
                            Text = pr.Name
                        };

                        e.Node.Nodes.Add(Property);
                    }
                }
                treeView1.EndUpdate();
            }


            if (arrType[1] == "Property")
            {
                return;

            }
        }
        private void InitRootGroup()
        {
            var rootGroup = _model.EnGroup.FirstOrDefault(x => x.Id == 1);
            if (rootGroup == null)
            {
                MessageBox.Show("Error");
                return;
            }
            var root = new TreeNode()
            {
                Text = rootGroup.Name,
                Name = rootGroup.Id.ToString() + "|Group"
            };
            var techGroup = new TreeNode()
            {
                Text = TechnicalGroupKey,
                Name = TechnicalGroupKey
            };
            root.Nodes.Add(techGroup);
            treeView1.Nodes.Add(root);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            _model.Database.Connection.Close();

        }
        private long CreateNewTestEntity(string name)
        {
            if (_model == null) return -1;
            var maxId = _model.EnGroup.Max(x => x.Id) + 1;
            var newEntity = new EnGroup()
            {
                Id = maxId,
                Name = name
            };
            _model.EnGroup.Add(newEntity);
            if (_model.SaveChanges() < 0)
                return -1;
            return maxId;
        }

        private void CreateNewTRelationEntity(long id, string name)
        {
            var selectedNode = treeView1.SelectedNode;
            if (selectedNode == null)
            {
                MessageBox.Show("Вы не выбрали элемент из дерева");
                return;
            }
            var arrNames = selectedNode.Name.Split('|');
            if (arrNames.Length < 2) MessageBox.Show("Неправильый формат данных в name");
            if (arrNames[1] == "Group")
            {
                var newEntity = new EnRelation
               {
                   Id_child = id,
                   Id_parent = long.Parse(arrNames[0])
               };
                _model.EnRelation.Add(newEntity);
                _model.SaveChanges();
            }
            if (arrNames[1] == "Property")
            {
                MessageBox.Show("Вы выбрали свойство");
            }
        }
        private void CreateProperty(string name, string value)
        {
            if (_model == null) return;
            var selectedNode = treeView1.SelectedNode;
            if (selectedNode==null)
            {
                MessageBox.Show("Вы не выбрали элемент");
                return;
            }
            var arrNames = selectedNode.Name.Split('|');
            var id = Convert.ToInt64(arrNames[0]);
            var maxId = _model.EnProperty.Max(x => x.Id) + 1;
            var Property = new EnProperty
            {
                Id = maxId,
                Name = name,
                Value = value,
                id_group = id
            };
            _model.EnProperty.Add(Property);
            _model.SaveChanges();
        }
        private void Delete(long id)
        {
            TreeNode selectedNode = treeView1.SelectedNode;
            if (selectedNode == null)
            {
                MessageBox.Show("Вы не выбрали элемент");
                return;
            }
            var name = selectedNode.Text;
            var id1 = selectedNode.Name;
            var arrNames = id1.Split('|');
            var help = Convert.ToInt64(arrNames[0]);
            if (arrNames.Length < 2) MessageBox.Show("Неправильый формат данных в name");
            if (arrNames[1] == "Group")
            {
                var GroupForDelete = _model.EnGroup.SingleOrDefault(x => x.Id == id);

                var properties = _model.EnProperty.Where(prop => prop.id_group == id);
                var childGroups = _model.EnRelation.Where(r => r.Id_parent == id);
                var parentRelation = _model.EnRelation.Where(r => r.Id_child == id);
                if (properties != null)
                {
                    _model.EnProperty.RemoveRange(properties);
                    var qq4 = _model.SaveChanges();
                }

                if (childGroups != null)
                {
                    foreach (var itemRel in childGroups)
                        _model.EnRelation.Remove(itemRel);

                    foreach (var iremCdhild in childGroups)
                    {
                        var groupForDel = _model.EnGroup.FirstOrDefault(x => x.Id == iremCdhild.Id_child);
                        if (groupForDel != null)
                            _model.EnGroup.Remove(groupForDel);
                    }
                    var qq3 = _model.SaveChanges();
                }

                if (parentRelation != null)
                {
                    _model.EnRelation.RemoveRange(parentRelation);
                    var qq2 = _model.SaveChanges();

                }

                if (GroupForDelete != null)
                {
                    _model.EnGroup.Remove(GroupForDelete);
                    var qq = _model.SaveChanges();

                }

            }
            if (arrNames[1] == "Property")
            {

                var PropertyForDelete = _model.EnProperty.SingleOrDefault(x => x.Id == id);
                _model.EnProperty.Remove(PropertyForDelete);
                _model.SaveChanges();
            }
            selectedNode.Collapse();
            selectedNode.Expand();
        }
        private void DeleteTestEntity(long id)
        {

            if (_model == null)
            {
                return;
            }
            var testEntityForDelete = _model.EnRelation.FirstOrDefault(x => x.Id_parent == id);
            if (testEntityForDelete == null)
            {
                return;
            }

            _model.EnRelation.Remove(testEntityForDelete);
        }
        private void UpdateS(string newName)
        {
            if (_model == null) return;
            TreeNode SelectedNode = treeView1.SelectedNode;
            if (SelectedNode == null)
            {
                MessageBox.Show("Вы не выбрали элемент");
                return;
            }
            var name = SelectedNode.Text;
            var updateGr = _model.EnGroup.SingleOrDefault(x => x.Name == name);
            updateGr.Name = newName;
            _model.SaveChanges();
            SelectedNode.Collapse();
            SelectedNode.Expand();

        }
        private void UpdateProperty(string newName,string newValue)
        {
            if (_model == null) return;
            TreeNode SelectedNode = treeView1.SelectedNode;
            if (SelectedNode == null)
            {
                MessageBox.Show("Вы не выбрали элемент");
                return;
            }
            var name = SelectedNode.Text;
            var updateProperty = _model.EnProperty.SingleOrDefault(x => x.Name == name);
            updateProperty.Name = newName;
            updateProperty.Value = newValue;
            _model.SaveChanges();
            SelectedNode.Collapse();
            SelectedNode.Expand();

        }

        private void UpdateRelation(long id_p, long id_c)
        {
            if (_model == null) return;
            var SelectedNode = treeView1.SelectedNode;
            if (SelectedNode == null)
            {
                MessageBox.Show("Вы не выбрали элемент из дерева");
                return;
            }
            var arrNames = SelectedNode.Name.Split('|');
            var update = _model.EnRelation.SingleOrDefault(x => x.Id_parent == id_p);
            update.Id_child = id_c;
            _model.SaveChanges();
        }
        private void button1_Click(object sender, EventArgs e)
        {


        }
        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {
            TreeNode node = treeView1.SelectedNode; 

            if (_groupOper == Operation.CREATE)
            {
                
                TreeNode SelectedNode = treeView1.SelectedNode;
                if (SelectedNode == null)
                {
                    MessageBox.Show("Вы не выбрали элемент из дерева");
                    return;
                }
                var arrNames = SelectedNode.Name.Split('|');
                if (arrNames[1] == "Group")
                {
                    var name = textBox1.Text;
                    var idNewGroup = CreateNewTestEntity(name);
                    if (idNewGroup < 0)
                    {
                        return;
                    }

                    CreateNewTRelationEntity(idNewGroup, name);
                    SelectedNode.Collapse();
                    SelectedNode.Expand();
                }
                
                textBox1.Text = null;
                textBox2.Text = null;
               
            }
 
            if (_groupOper == Operation.UPDATE)
            {
                TreeNode SelectedNode = treeView1.SelectedNode;
                if (SelectedNode == null)
                {
                    MessageBox.Show("Вы не выбрали элемент из дерева");
                    return;
                }
                var newname = textBox1.Text;
                UpdateS(newname);
                treeView1.CollapseAll();
                treeView1.SelectedNode.Expand();
                textBox1.Text = null;
                textBox2.Text = null;

               
            }
            if (_groupOper == Operation.NONE)
            {
                Show1(false);
                Show2(false);
            }
           

            MessageBox.Show("Изменения сохранены");
        }

        private void button2_Click(object sender, EventArgs e)
        {


        }

        private void button3_Click(object sender, EventArgs e)
        {

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
     
        }

        private void button6_Click(object sender, EventArgs e)
        {

        }
        void Show1(bool visible)
        {
            label1.Visible = visible;
            label2.Visible = visible;
            label3.Visible = visible;
            textBox1.Visible = visible;
            textBox2.Visible = visible;
            button5.Visible = visible;
            button6.Visible = visible;
        }
        void Show2(bool visible)
        {
            label4.Visible = visible;
            label5.Visible = visible;
            label6.Visible = visible;
            label7.Visible = visible;
            textBox3.Visible = visible;
            textBox4.Visible = visible;
            textBox5.Visible = visible;
            button7.Visible = visible;
            button4.Visible = visible;
        }
        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {

        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {


        }

        private void группаToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _groupOper = Operation.CREATE;
            Show1(true);
            Show2(false);
            Enable(false);

        }

        private void свойствоToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _groupOper = Operation.CREATE;
            Show1(false);
            Show2(true);
            Enable(false);
        }

        private void свойствоToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            Show1(false);
            Show2(true);
        }

        private void группаToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            _groupOper = Operation.DELETE;
            Show1(false);
            Show2(false);
        }

        private void свойствоToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Show1(false);
            Show2(true);
        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void изменитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            _groupOper = Operation.UPDATE;
            Enable(false);
            if (_model == null) return;
            var SelectedNode = treeView1.SelectedNode;
            if (SelectedNode == null)
            {
                MessageBox.Show("Вы не выбрали элемент из дерева");
                return;
            }
            var arrNames = SelectedNode.Name.Split('|');
            var id=Convert.ToInt64(arrNames[0]);
            if (arrNames.Length < 2) MessageBox.Show("Неправильый формат данных в name");
            if (arrNames[1] == "Group")
            {
                Show1(true);
                Show2(false);
                textBox1.Text = SelectedNode.Text;
                textBox2.Text = arrNames[0];
            }
            if (arrNames[1] == "Property")
            {
                Show2(true);
                Show1(false);
                textBox3.Text = SelectedNode.Text;
                textBox5.Text = arrNames[0];
                var value = _model.EnProperty.FirstOrDefault(x => x.Id ==id);
                textBox4.Text = value.Value;
                
                
            }
        }

        private void группаToolStripMenuItem_Click()
        {

        }

        private void button7_Click(object sender, EventArgs e)
        {
            textBox3.Text = null;
            textBox4.Text = null;
            textBox5.Text = null;
            Show2(false);
        }

        private void button6_Click_1(object sender, EventArgs e)
        {
            textBox1.Text = null;
            textBox2.Text = null;
            Show1(false);
        }

        private void удалитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TreeNode node = treeView1.SelectedNode;
            _groupOper = Operation.DELETE;
            Show1(false);
            Show2(false);
            var selectedNode = treeView1.SelectedNode;
            if (selectedNode == null)
            {
                MessageBox.Show("Вы не выбрали элемент");
            }
            var name = selectedNode.Name;
            var arrNames = selectedNode.Name.Split('|');
            var id = arrNames[0];
            Delete(long.Parse(id));
            treeView1.CollapseAll();
            treeView1.SelectedNode.Expand();
     
            MessageBox.Show("Изменения сохранены");

  
        }

        private void button4_Click(object sender, EventArgs e)
        {
            TreeNode node = treeView1.SelectedNode; 

            if (_groupOper == Operation.CREATE)
            {
                var SelectedNode = treeView1.SelectedNode;
                if (SelectedNode == null)
                {
                    MessageBox.Show("Вы не выбрали элемент из дерева");
                    return;
                }
                var arrNames = SelectedNode.Name.Split('|');
                    var name = textBox3.Text;
                    var value = textBox4.Text;
                    CreateProperty(name, value); 
                    MessageBox.Show("Изменения сохранены");
                    textBox4.Text = null;
                    textBox3.Text = null;
                    textBox5.Text = null;
                    node.Collapse();
                    node.Expand();

            }
            if (_groupOper==Operation.UPDATE)
            {
                var name = textBox3.Text;
                var value = textBox4.Text;
                UpdateProperty(name, value);
                MessageBox.Show("Изменения сохранены");
                textBox4.Text = null;
                textBox3.Text = null;
                textBox5.Text = null;
                treeView1.CollapseAll();
                treeView1.SelectedNode.Expand();
            }
            
            
           
        }
      void Enable (bool enable)
        {
            textBox2.Enabled = enable;
            textBox5.Enabled = enable;
        }
        [Table("TGroup")]
        public class EnGroup
        {
            [Key, DatabaseGenerated(System.ComponentModel.DataAnnotations.Schema.DatabaseGeneratedOption.None)]
            [Column("Id")]
            public long Id { get; set; }
            [Column("Name")]
            public string Name { get; set; }
        }
        [Table("TRelation")]
        public class EnRelation
        {
            [Key]
            [Column("Id_parent", Order = 1)]
            public long Id_parent { get; set; }
            [Key]
            [Column("Id_child", Order = 2)]
            public long Id_child { get; set; }
        }
        [Table("TProperty")]
        public class EnProperty
        {
            [Key]
            [Column("id_property", Order = 1)]
            public long Id { get; set; }
            [Column("Name")]
            public string Name { get; set; }
            [Column("Value")]
            public string Value { get; set; }
            [Key]
            [Column("id_group", Order = 2)]
            public long id_group { get; set; }
        }
        public partial class ModelContext : DbContext
        {
            public ModelContext()
                : base("ModelConnection")
            {

            }
            public virtual DbSet<EnGroup> EnGroup { get; set; }
            public virtual DbSet<EnRelation> EnRelation { get; set; }
            public virtual DbSet<EnProperty> EnProperty { get; set; }

        }

        public class CustomGroup
        {
            public string Name { get; set; }
            public long Id { get; set; }
        }
        public class CustomGroup2
        {
            public string Name { get; set; }
            public long Id { get; set; }
        }

        public enum Operation
        {
            CREATE,
            UPDATE,
            DELETE,
            NONE
        }

    }
}