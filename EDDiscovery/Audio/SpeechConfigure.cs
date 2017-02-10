﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EDDiscovery.Audio
{
    public partial class SpeechConfigure : Form
    {
        public bool Wait { get { return checkBoxCustomComplete.Checked; } }
        public bool Preempt { get { return checkBoxCustomPreempt.Checked; } }
        public string SayText { get { return textBoxBorderText.Text; } }
        public string VoiceName { get { return comboBoxCustomVoice.Text; } }
        public string Volume { get { return (checkBoxCustomV.Checked) ? trackBarVolume.Value.ToString() : "Default"; } }
        public string Rate { get { return (checkBoxCustomR.Checked) ? trackBarRate.Value.ToString() : "Default"; } }
        public ConditionVariables Effects { get { return effects;  } }

        AudioQueue queue;
        SpeechSynthesizer synth;
        ConditionVariables effects;

        public SpeechConfigure()
        {
            InitializeComponent();
        }

        public void Init( AudioQueue qu, SpeechSynthesizer syn,
                            string title, string caption , EDDiscovery2.EDDTheme theme,
                            String text,          // if null, no text box or wait complete
                            bool waitcomplete, bool preempt,
                            string voicename,
                            string volume,
                            string rate,
                            ConditionVariables ef)     // effects can also contain other vars, it will ignore
        {
            queue = qu;
            synth = syn;
            this.Text = caption;
            Title.Text = title;
            textBoxBorderTest.Text = "The quick brown fox jumped over the lazy dog";

            bool defaultmode = (text == null);

            if (defaultmode)
            {
                textBoxBorderText.Visible = checkBoxCustomComplete.Visible = checkBoxCustomPreempt.Visible = false;
                checkBoxCustomV.Visible = checkBoxCustomR.Visible = false;

                int offset = comboBoxCustomVoice.Top - textBoxBorderText.Top;
                foreach (Control c in panelOuter.Controls )
                {
                    if (!c.Name.Equals("Title"))
                        c.Location = new Point(c.Left, c.Top - offset);
                }

                this.Height -= offset;
            }
            else
            {
                textBoxBorderText.Text = text;
                checkBoxCustomComplete.Checked = waitcomplete;
                checkBoxCustomPreempt.Checked = preempt;
            }

            comboBoxCustomVoice.Items.Add("Default");
            comboBoxCustomVoice.Items.Add("Female");
            comboBoxCustomVoice.Items.Add("Male");
            comboBoxCustomVoice.Items.AddRange(synth.GetVoiceNames());
            comboBoxCustomVoice.SelectedItem = voicename;

            int i;
            if (!defaultmode && volume.Equals("Default", StringComparison.InvariantCultureIgnoreCase))  
            {
                checkBoxCustomV.Checked = false;
                trackBarVolume.Enabled = false;
            }
            else
            {
                checkBoxCustomV.Checked = true;
                if (volume.InvariantParse(out i))
                    trackBarVolume.Value = i;
            }

            if (!defaultmode && rate.Equals("Default", StringComparison.InvariantCultureIgnoreCase))
            {
                checkBoxCustomR.Checked = false;
                trackBarRate.Enabled = false;
            }
            else
            {
                checkBoxCustomR.Checked = true;
                if (rate.InvariantParse(out i))
                    trackBarRate.Value = i;
            }

            effects = ef;

            theme.ApplyToForm(this, System.Drawing.SystemFonts.DefaultFont);
        }

        private void buttonExtOK_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
            Close();
        }

        private void buttonExtEffects_Click(object sender, EventArgs e)
        {
            SoundEffectsDialog sfe = new SoundEffectsDialog();
            sfe.Init(effects);
            sfe.TestSettingEvent += Sfe_TestSettingEvent;           // callback to say test
            sfe.StopTestSettingEvent += Sfe_StopTestSettingEvent;   // callback to say stop
            if ( sfe.ShowDialog(this) == DialogResult.OK )
            {
                effects = sfe.GetEffects();
            }
        }

        private void Sfe_TestSettingEvent(SoundEffectsDialog sfe, ConditionVariables effects)
        {
            string errlist;
            System.IO.MemoryStream ms = synth.Speak(textBoxBorderTest.Text, comboBoxCustomVoice.Text, trackBarRate.Value, out errlist);
            if (ms != null)
            {
                AudioQueue.AudioSample a = queue.Generate(ms, effects);
                a.sampleOverEvent += SampleOver;
                a.sampleOverTag = sfe;
                queue.Submit(a, trackBarVolume.Value);
            }
        }

        private void Sfe_StopTestSettingEvent(SoundEffectsDialog sender)
        {
            queue.StopCurrent();
        }

        private void SampleOver(AudioQueue s, Object tag)
        {
            SoundEffectsDialog sfe = tag as SoundEffectsDialog;
            sfe.TestOver();
        }

        private void checkBoxCustomV_CheckedChanged(object sender, EventArgs e)
        {
            trackBarVolume.Enabled = checkBoxCustomV.Checked;

        }
        private void checkBoxCustomR_CheckedChanged(object sender, EventArgs e)
        {
            trackBarRate.Enabled = checkBoxCustomR.Checked;
        }

        private void buttonExtTest_Click(object sender, EventArgs e)
        {
            if (buttonExtTest.Text.Equals("Stop"))
            {
                queue.StopAll();
                buttonExtTest.Text = "Test";
            }
            else
            {
                try
                {
                    string errlist;
                    System.IO.MemoryStream ms = synth.Speak(textBoxBorderTest.Text, comboBoxCustomVoice.Text, trackBarRate.Value, out errlist);
                    if (ms != null)
                    {
                        queue.Submit(queue.Generate(ms, effects), trackBarVolume.Value);
                        buttonExtTest.Text = "Stop";
                    }
                }
                catch
                {
                    MessageBox.Show("Unable to play " + textBoxBorderText.Text);
                }
            }
        }
    }
}