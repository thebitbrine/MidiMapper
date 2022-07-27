using Melanchall.DryWetMidi.Multimedia;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsInput;
using WindowsInput.Native;

namespace MidiMapper
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            this.KeyPreview = true;
            KeyHandler.Instance.Init();
            this.KeyDown += Form1_KeyDown;
            this.KeyUp += Form1_KeyUp;

            KeyHandler.Instance.KeyPressed += Instance_KeyPressed;

            listBox1.DrawItem += new DrawItemEventHandler(listBox_DrawItem);
            Directory.CreateDirectory("Bindings");
        }
        void listBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            ListBox list = (ListBox)sender;
            if (e.Index > -1)
            {
                object item = list.Items[e.Index];
                e.DrawBackground();
                e.DrawFocusRectangle();
                Brush brush = new SolidBrush(e.ForeColor);
                SizeF size = e.Graphics.MeasureString(item.ToString(), e.Font);
                e.Graphics.DrawString(item.ToString(), e.Font, brush, e.Bounds.Left + (e.Bounds.Width / 2 - size.Width / 2), e.Bounds.Top + (e.Bounds.Height / 2 - size.Height / 2));
            }
        }

        public string LastMidiInput = null;
        public KeyInput LastKeyInput = new KeyInput();
        private void Instance_KeyPressed(object sender, KeyEventArgs e)
        {
            LastKeyInput = new KeyInput()
            {
                Key = e
            };
            if (e.Modifiers != Keys.None)
            {
                var KeyName = "";
                var xxx = e.Modifiers;
                LastKeyInput.Name = xxx.ToString().Replace(",", " +") + " + " + e.KeyCode;

            }
            else
            {
               LastKeyInput.Name = e.KeyCode.ToString();
            }
            LastKeyInput.Name = LastKeyInput.Name.Replace(" + Menu", "");
        }

        private void Form1_KeyUp(object sender, KeyEventArgs e)
        {
            // Fire event when a key is released
            KeyHandler.Instance.FireKeyUp(sender, e);
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            // Fire event when a key is pressed
            KeyHandler.Instance.FireKeyDown(sender, e);
        }


        private static InputDevice _inputDevice;

        private void Form1_Load(object sender, EventArgs e)
        {

            comboBox1.Items.Add("None");
            var AllDevices = InputDevice.GetAll();
            comboBox1.Items.AddRange(AllDevices.Select(x => x.Name).ToArray());
            comboBox1.SelectedIndex = 1;
        }

        public InputSimulator x = new InputSimulator();
        public bool SimulatingKey = false;
        public string SimKeyName = "";
        private void OnEventReceived(object sender, MidiEventReceivedEventArgs e)
        {
            var midiDevice = (MidiDevice)sender;
            Console.WriteLine($"Event received from '{midiDevice.Name}' at {DateTime.Now}: {e.Event}");
            LastMidiInput = e.Event.ToString().Split('(').Last().Split(',').First().Trim(')').Trim();
            if (Bindings.ContainsKey(LastMidiInput) && e.Event.EventType != Melanchall.DryWetMidi.Core.MidiEventType.NoteOff)
            {
                var SimKey = Bindings[LastMidiInput].Key;
                SimKeyName = Bindings[LastMidiInput].Name;
                var Mods = SimKey.Modifiers.ToString().Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()).ToList();
                var FinalMods = new List<VirtualKeyCode>();
                foreach (var Mod in Mods)
                {
                    var Key = -1;
                    switch (Mod.Replace("ControlKey", "Control").Replace("Menu", ""))
                    {
                        case "Shift":
                            Key = (int)VirtualKeyCode.SHIFT;
                            break;
                        case "Control":
                            Key = (int)VirtualKeyCode.CONTROL;
                            break;
                        case "Alt":
                            Key = (int)VirtualKeyCode.MENU;
                            break;

                        default:
                            break;
                    }
                    if (Key > 0)
                        FinalMods.Add((VirtualKeyCode)Key);
                }
                SimulatingKey = true;
                x.Keyboard.ModifiedKeyStroke(FinalMods, (VirtualKeyCode)SimKey.KeyCode);

            }
        }

        private void comboBox1_SelectedValueChanged(object sender, EventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(comboBox1.Text))
            {
                try
                {
                    _inputDevice = InputDevice.GetByName(comboBox1.Text);
                    _inputDevice.EventReceived += OnEventReceived;
                    _inputDevice.StartEventsListening();
                    try
                    {
                        var Data = File.ReadAllText(Path.Combine("Bindings", comboBox1.Text + ".json"));
                        Bindings = JsonConvert.DeserializeObject<Dictionary<string, KeyInput>>(Data);
                        foreach (var Binding in Bindings)
                        {
                            listBox1.Items.Add(Binding.Key + " >> " + Binding.Value.Name);
                        }
                    }
                    catch { }
                    button2.Enabled = true;
                    comboBox1.Enabled = false;
                }
                catch { }
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            label3.Text = LastMidiInput;
            label4.Text = LastKeyInput.Name;
            if(LastKeyInput.Key != null && LastMidiInput != null && !Bindings.Keys.Any(x => x == LastMidiInput))
            {
                button3.Enabled = true;
            }
            else
            {
                button3.Enabled = false;
            }

            if (listBox1.SelectedItem != null)
            {
                button1.Enabled = true;
            }
            else
            {
                button1.Enabled = false;
            }
            if (SimulatingKey)
            {

                label7.Text = SimKeyName;
                panel1.Dock = DockStyle.Fill;
                SimulatingKey = false;
            }
            else
            {
                panel1.Dock = DockStyle.None;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            _inputDevice.StopEventsListening();
            _inputDevice.Dispose();
            button2.Enabled = false;
            comboBox1.Enabled = true;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                Bindings.Add(LastMidiInput, LastKeyInput);
                listBox1.Items.Add(LastMidiInput + " >> " + LastKeyInput.Name);
                listBox1.SelectedIndex = listBox1.Items.Count - 1;
                try { File.WriteAllText(Path.Combine("Bindings", comboBox1.Text + ".json"), JsonConvert.SerializeObject(Bindings)); } catch { }
            }
            catch { }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            try
            {
                Bindings.Remove(listBox1.SelectedItem.ToString().Split(new string[] {" >> "}, StringSplitOptions.RemoveEmptyEntries).First().Trim());
                var SelectedIndex = listBox1.SelectedIndex;
                listBox1.Items.RemoveAt(listBox1.SelectedIndex);
                if (listBox1.Items.Count > 0)
                    listBox1.SelectedIndex = Math.Max(SelectedIndex - 1, 0);

                try { File.WriteAllText(Path.Combine("Bindings", comboBox1.Text + ".json"), JsonConvert.SerializeObject(Bindings)); } catch { }
            }
            catch { }
        }
        public Dictionary<string, KeyInput> Bindings = new Dictionary<string, KeyInput>();
        public class MidiInput
        {
            public string Name;
            public string Note;
        }
        public class KeyInput
        {
            public string Name;
            public KeyEventArgs Key;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }
}
