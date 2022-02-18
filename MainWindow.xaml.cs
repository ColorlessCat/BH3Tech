using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace 对崩坏科研3
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const int STATE_UNSCAN = 0;
        const int STATE_SCANNED = 1;
        const int STATE_SCANING = 2;
        const int STATE_UNHACK = 3;
        const int STATE_WAIT_TO_CHANGE =4;
        const int STATE_RECOVERING = 5;
        public int currentState = STATE_UNSCAN;

        public List<Weapon> weapons=new List<Weapon>();
        public List<Weapon> weaponsHacked = new List<Weapon>();
        public int lastSelectedItemIndex = -1;
        
        MemoryScanner ms;
        JsonHandler jh;
        public MainWindow()
        {
            WindowBlur.SetIsEnabled(this, true);
            WPFUI.Background.Manager.Apply(this);
            InitializeComponent();
            try
            {
                ms = new MemoryScanner();
                jh= new JsonHandler();
                List<Weapon>? tl = jh.readConfig();
                if (tl != null) {
                    weapons.AddRange(tl);
                }
                
            }catch(Exception ex)
            {
                //TODO 后面换成UI库的MB
                MessageBox.Show("初始化失败，检查是否开启了管理员权限和游戏(并且配置文件齐全)。\n 错误信息："+ex.Message);
                Environment.Exit(0);
            }
            
            InitView();
            InitAddress();

        }

        private void CloneList(List<Weapon> origin,List<Weapon> clone)
        {
            foreach(Weapon weapon in origin)
                clone.Add(weapon.copy());
        }

        private void InitView()
        {
            weaponList.Items.Clear();
            foreach (Weapon weapon in weapons)
            weaponList.Items.Add(weapon);

            skill1.Items.Add(new Skill());
            skill2.Items.Add(new Skill());
            skill3.Items.Add(new Skill() { skillID = 3055, skillName = "碰我就死" });
            skill1.SelectedIndex = 0;
            skill2.SelectedIndex = 0;
            skill3.SelectedIndex = 0;

        }

        private void InitAddress(int index=0)
        {
            if (weapons.Count == 0) return;
            currentState = STATE_SCANING;
            state.Text = "扫描中...";
           
            ms.AOBScan4BH3MultiThread(weapons[index].traitCode, weapons[index].addressLow3, delegate (float progress, int value) {
                if (value != -1)
                    weapons[index].address = value;
                if (progress != 1f) return;
                
                Interlocked.Increment(ref index);
                UpdateProgress((int)(index * 1f / weapons.Count * 100));
                if (index >= weapons.Count) {
                    InitWeaponInfo();
                    return; 
                }
                Dispatcher.BeginInvoke(delegate()
                {
                    InitAddress(index);
                });
            });
            
        }

        private void InitWeaponInfo()
        {
          for (int i = 0;i < weapons.Count;i++)
            {
                Weapon weapon = weapons[i];
                if (weapon.address == -1) continue;
                weapon.ack = ms.ReadMemory<float>(weapon.address+Weapon.OFFSET_ACK);
            
                weapon.crit = ms.ReadMemory<float>(weapon.address+Weapon.OFFSET_CRIT);
                weapon.defend = ms.ReadMemory<float>(weapon.address+Weapon.OFFSET_DEFENDE);
                weapon.hp = ms.ReadMemory<float>(weapon.address+Weapon.OFFSET_HP);
                weapon.skillCodes = new int[3];
                weapon.skillCodes[0] = ms.ReadMemory<int>(weapon.address + Weapon.OFFSET_SKILL1);
                weapon.skillCodes[1] = ms.ReadMemory<int>(weapon.address + Weapon.OFFSET_SKILL2);
                weapon.skillCodes[2] = ms.ReadMemory<int>(weapon.address + Weapon.OFFSET_SKILL3);
                weapon.skillValuesFloat = new float[3][];
                weapon.skillValuesFloat[0]= new float[3];
                weapon.skillValuesFloat[1] = new float[3];
                weapon.skillValuesFloat[2] = new float[3];
                weapon.skillValuesFloat[0][0] = ms.ReadMemory<float>(weapon.address + Weapon.OFFSET_SKILL1_VALUES);
                weapon.skillValuesFloat[0][1] = ms.ReadMemory <float>(weapon.address + Weapon.OFFSET_SKILL1_VALUES+4);
                weapon.skillValuesFloat[0][2] = ms.ReadMemory<float>(weapon.address + Weapon.OFFSET_SKILL1_VALUES + 8);

                weapon.skillValuesFloat[1][0] = ms.ReadMemory<float>(weapon.address + Weapon.OFFSET_SKILL2_VALUES);
                weapon.skillValuesFloat[1][1] = ms.ReadMemory<float>(weapon.address + Weapon.OFFSET_SKILL2_VALUES + 4);
                weapon.skillValuesFloat[1][2] = ms.ReadMemory<float>(weapon.address + Weapon.OFFSET_SKILL2_VALUES + 8); 

                weapon.skillValuesFloat[2][0] = ms.ReadMemory<float>(weapon.address + Weapon.OFFSET_SKILL3_VALUES);
                weapon.skillValuesFloat[2][1] = ms.ReadMemory<float>(weapon.address + Weapon.OFFSET_SKILL3_VALUES + 4);
                weapon.skillValuesFloat[2][2] = ms.ReadMemory<float>(weapon.address + Weapon.OFFSET_SKILL3_VALUES+8);

                weapon.skillValuesBytes = new byte[72];
                for (int j = 0; j < weapon.skillValuesBytes.Length; j++) 
                    weapon.skillValuesBytes[j] = ms.ReadMemory<byte>(weapon.address + Weapon.OFFSET_SKILL1_VALUES+j);
                weapon.allDataBytes = new byte[128];
                for (int j = 0; j < weapon.allDataBytes.Length; j++)
                    weapon.allDataBytes[j] = ms.ReadMemory<byte>(weapon.address +j);
                //TODO 不确定是否可以更新
                Dispatcher.BeginInvoke(delegate ()
                {
                    if (weaponList.SelectedIndex == i)
                    {
                        weaponList.Items[i] = weapon;
                        weaponList.SelectedIndex = i;
                    }
                });

            }
            CloneList(weapons, weaponsHacked);
        }

        private void UpdateProgress(int value)
        {
            this.Dispatcher.BeginInvoke(new Action(() => {
                progressBar.Value = value;
                if (value >= 100)
                {
                    currentState = STATE_SCANNED;
                    state.Text = "扫描完成";
                }
            }));
           
        }

        private void SaveValuesFromView(int i) {
            //如果没有搜到地址就无法修改 保存要修改的值也无意义
            if (currentState == STATE_SCANING) return;
            if (weaponsHacked[i].address == -1) return;
            weaponsHacked[i].ack = float.Parse(ack.Text);
            weaponsHacked[i].crit = float.Parse(crit.Text);
            weaponsHacked[i].defend = float.Parse(defend.Text);
            weaponsHacked[i].hp = float.Parse(hp.Text);

            weaponsHacked[i].skillCodes[0] = (skill1.SelectedItem as Skill).skillID;
            weaponsHacked[i].skillCodes[1] =(skill2.SelectedItem as Skill).skillID;
            weaponsHacked[i].skillCodes[2] = (skill3.SelectedItem as Skill).skillID;

            weaponsHacked[i].skillValuesFloat[0][0] = float.Parse(skill1Value1.Text);
            weaponsHacked[i].skillValuesFloat[0][1] = float.Parse(skill1Value2.Text);
            weaponsHacked[i].skillValuesFloat[0][2] = float.Parse(skill1Value3.Text);

            weaponsHacked[i].skillValuesFloat[1][0] = float.Parse(skill2Value1.Text);
            weaponsHacked[i].skillValuesFloat[1][1] = float.Parse(skill2Value2.Text);
            weaponsHacked[i].skillValuesFloat[1][2] = float.Parse(skill2Value3.Text);

            weaponsHacked[i].skillValuesFloat[2][0] = float.Parse(skill3Value1.Text);
            weaponsHacked[i].skillValuesFloat[2][1] = float.Parse(skill3Value2.Text);
            weaponsHacked[i].skillValuesFloat[2][2] = float.Parse(skill3Value3.Text);
        }
        private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (weaponList.Items.Count != weapons.Count) return;
            //在选择变更以前 先保存上一个选择的输入
            if (lastSelectedItemIndex != -1&&weapons.Count!=0) {
                SaveValuesFromView(lastSelectedItemIndex);
               
            }
            lastSelectedItemIndex = weaponList.SelectedIndex;

            Weapon weapon = weaponList.SelectedItem as Weapon;
            weapon= weaponsHacked.Count==0? weapon : weaponsHacked[weaponList.SelectedIndex];
            if(weapon.traitCode!=null)
            tritCode.Text = Tool.byteArray2String(weapon.traitCode);
            address.Text = weapon.address.ToString("X2");
          
            if (currentState < STATE_SCANNED) return;
            ack.Text = weapon.ack.ToString();
            crit.Text = weapon.crit.ToString();
            defend.Text = weapon.defend.ToString();
            hp.Text = weapon.hp.ToString();
            if (weapon.skillCodes == null) return;
            //TODO 做个配置文件 定义技能ID和技能名以及描述的映射
            (skill1.SelectedItem as Skill).skillID = weapon.skillCodes[0];
            (skill2.SelectedItem as Skill).skillID = weapon.skillCodes[1];
            (skill3.SelectedItem as Skill).skillID = weapon.skillCodes[2];
            for (int i =0; weapon.skillValuesFloat != null&&i < weapon.skillValuesFloat.Length;i++)
                {
                    if (i  == 0)
                    {
                        skill1Value1.Text = weapon.skillValuesFloat[i][0].ToString();
                        skill1Value2.Text = weapon.skillValuesFloat[i][1].ToString();
                        skill1Value3.Text = weapon.skillValuesFloat[i][2].ToString();
                    }
                    else if (i  == 1) {
                        skill2Value1.Text = weapon.skillValuesFloat[i][0].ToString();
                        skill2Value2.Text = weapon.skillValuesFloat[i][1].ToString();
                        skill2Value3.Text = weapon.skillValuesFloat[i][2].ToString();
                    }
                    else if (i == 2)
                    {
                        skill3Value1.Text = weapon.skillValuesFloat[i][0].ToString();
                        skill3Value2.Text = weapon.skillValuesFloat[i][1].ToString();
                        skill3Value3.Text = weapon.skillValuesFloat[i][2].ToString();
                    }
                
                
            }
        }

        private void OnSearchClick(object sender, RoutedEventArgs e)
        {
            

        }

        private int HackData(List<Weapon> list)
        {
            int dataChanged=0;
            for (int i = 0; i < list.Count; i++)
            {
                Weapon w = list[i];
                if (w.address == -1) continue;
                ms.WriteMemory(w.address + Weapon.OFFSET_ACK, w.ack);
                ms.WriteMemory(w.address + Weapon.OFFSET_CRIT, w.crit);
                ms.WriteMemory(w.address + Weapon.OFFSET_DEFENDE, w.defend);
                ms.WriteMemory(w.address + Weapon.OFFSET_HP, w.hp);
                ms.WriteMemory(w.address + Weapon.OFFSET_SKILL1, w.skillCodes[0]);
                ms.WriteMemory(w.address + Weapon.OFFSET_SKILL2, w.skillCodes[1]);
                ms.WriteMemory(w.address + Weapon.OFFSET_SKILL3, w.skillCodes[2]);

                ms.WriteMemory(w.address + Weapon.OFFSET_SKILL1_VALUES, w.skillValuesFloat[0][0]);
                ms.WriteMemory(w.address + Weapon.OFFSET_SKILL1_VALUES + 4, w.skillValuesFloat[0][1]);
                ms.WriteMemory(w.address + Weapon.OFFSET_SKILL1_VALUES + 8, w.skillValuesFloat[0][2]);

                ms.WriteMemory(w.address + Weapon.OFFSET_SKILL2_VALUES, w.skillValuesFloat[1][0]);
                ms.WriteMemory(w.address + Weapon.OFFSET_SKILL2_VALUES + 4, w.skillValuesFloat[1][1]);
                ms.WriteMemory(w.address + Weapon.OFFSET_SKILL2_VALUES + 8, w.skillValuesFloat[1][2]);

                ms.WriteMemory(w.address + Weapon.OFFSET_SKILL3_VALUES, w.skillValuesFloat[2][0]);
                ms.WriteMemory(w.address + Weapon.OFFSET_SKILL3_VALUES + 4, w.skillValuesFloat[2][1]);
                ms.WriteMemory(w.address + Weapon.OFFSET_SKILL3_VALUES + 8, w.skillValuesFloat[2][2]);

                dataChanged++;
            }
            return dataChanged;
        }
        private void OnChangeClick(object sender, RoutedEventArgs e)
        {
            SaveValuesFromView(weaponList.SelectedIndex);
            int hackedCount = HackData(weaponsHacked);
            if (hackedCount == 0) {
                MessageBox.Show("无可用地址，未进行任何修改。");
                return;
            }
          
            currentState = STATE_RECOVERING;
           
            new Thread(() => {
                int cd = 10;
                for (; cd >= 0; cd--) {
                    Dispatcher.BeginInvoke(new Action(() => {
                        if (cd == 0)
                        {
                         
                            RecoveryData();
                            return;
                            
                        }
                        state.Text = cd + " 秒后还原。";
                       
                    }));
                    Thread.Sleep(1000);
                }

                
            }).Start();
        }

        private void RecoveryData()
        {
           int i=ReductionData(weapons);
            if (i == 0) {
                MessageBox.Show("未知错误，还原失败。");
                return;
            }
            currentState = STATE_WAIT_TO_CHANGE;
            state.Text = "等待修改。";
            
        }

        private int ReductionData(List<Weapon> list)
        {
            int dataChanged = 0;
            for (int i = 0; i < list.Count; i++)
            {
                Weapon w = list[i];
                if (w.address == -1) continue;
                ms.WriteMemory(w.address + Weapon.OFFSET_ACK, w.ack);
                ms.WriteMemory(w.address + Weapon.OFFSET_CRIT, w.crit);
                ms.WriteMemory(w.address + Weapon.OFFSET_DEFENDE, w.defend);
                ms.WriteMemory(w.address + Weapon.OFFSET_HP, w.hp);
                ms.WriteMemory(w.address + Weapon.OFFSET_SKILL1, w.skillCodes[0]);
                ms.WriteMemory(w.address + Weapon.OFFSET_SKILL2, w.skillCodes[1]);
                ms.WriteMemory(w.address + Weapon.OFFSET_SKILL3, w.skillCodes[2]);

                for (int j = 0; j < w.allDataBytes.Length; j++) {
                    ms.WriteMemory(w.address + j, w.allDataBytes[j]);
                }
               
                dataChanged++;
            }
            return dataChanged;
        }

        private void OnConfirmClickDialog(object sender, RoutedEventArgs e)
        {
            String name = configName.Text;
            String tritCode = configTric.Text;
            String low3= configLow3.Text;
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(tritCode)||name.Equals("名字")||tritCode.Equals("特征码")) {
                MessageBox.Show("输入有误。");
                return;
            }
            Weapon weapon = new Weapon();
            weapon.name = name;
            weapon.traitCode = Tool.string2ByteArray(tritCode);
            weapon.addressLow3 = low3.Equals("地址低三位") ? 0x00000fff:int.Parse(low3,System.Globalization.NumberStyles.HexNumber);
            if (!jh.appendConfigToFile(weapon)) {
                MessageBox.Show("添加失败。");
                return;
            }
            weapons.Add(weapon);
            weaponsHacked.Add(weapon);
            InitView();
            dialog.Show = false;
        }

        private void GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb.Name .Equals( "configTric")&&tb.Text.Equals("特征码")) {
                tb.Opacity = 1;
                tb.Text = "";
            }
            else if (tb.Name.Equals("configName") && tb.Text.Equals("名字"))
            {
                tb.Opacity = 1;
                tb.Text = "";
            }
            else if (tb.Name.Equals("configLow3") && tb.Text.Equals("地址低三位"))
            {
                tb.Opacity = 1;
                tb.Text = "";
            }

        }

        private void LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            if (tb.Name.Equals("configTric") && tb.Text.Equals(""))
            {
                tb.Opacity = 0.75;
                tb.Text = "特征码";
            }
            else if (tb.Name.Equals("configName") && tb.Text.Equals(""))
            {
                tb.Opacity = 0.75;
                tb.Text = "名字";
            }
            else if (tb.Name.Equals("configLow3") && tb.Text.Equals(""))
            {
                tb.Opacity = 0.75;
                tb.Text = "地址低三位";
            }
        }

        private void AddConfig(object sender, RoutedEventArgs e)
        {
            dialog.Show = true;
        }
    }
}
