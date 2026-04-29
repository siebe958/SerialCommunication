using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace SerialCommunication
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                string[] portNames = SerialPort.GetPortNames().Distinct().ToArray();
                comboBoxPoort.Items.Clear();
                comboBoxPoort.Items.AddRange(portNames);
                if (comboBoxPoort.Items.Count > 0) comboBoxPoort.SelectedIndex = 0;

                comboBoxBaudrate.SelectedIndex = comboBoxBaudrate.Items.IndexOf("115200");
            }
            catch (Exception)
            { }
        }

        private void cboPoort_DropDown(object sender, EventArgs e)
        {
            try
            {
                string selected = (string)comboBoxPoort.SelectedItem;
                string[] portNames = SerialPort.GetPortNames().Distinct().ToArray();

                comboBoxPoort.Items.Clear();
                comboBoxPoort.Items.AddRange(portNames);

                comboBoxPoort.SelectedIndex = comboBoxPoort.Items.IndexOf(selected);
            }
            catch (Exception)
            {
                if (comboBoxPoort.Items.Count > 0) comboBoxPoort.SelectedIndex = 0;
            }
        }

        private void buttonConnect_Click(object sender, EventArgs e)
        {
            try
            {
                serialPortarduino.ReadTimeout = 2000; // Wacht max 2 seconden op antwoord

                if (serialPortarduino.IsOpen)
                {
                    serialPortarduino.Close();
                    radioButtonVerbonden.Checked = false;
                    buttonConnect.Text = "Connect";
                    labelStatus.Text = "status : disconnected";
                }
                else
                {
                    serialPortarduino.PortName = (string)comboBoxPoort.SelectedItem;
                    serialPortarduino.BaudRate = Int32.Parse((string)comboBoxBaudrate.SelectedItem);
                    serialPortarduino.DataBits = (int)numericUpDownDatabits.Value;

                    if (radioButtonParityEven.Checked) serialPortarduino.Parity = Parity.Even;
                    else if (radioButtonParityMark.Checked) serialPortarduino.Parity = Parity.Mark;
                    else if (radioButtonParityNone.Checked) serialPortarduino.Parity = Parity.None;
                    else if (radioButtonParityOdd.Checked) serialPortarduino.Parity = Parity.Odd;
                    else if (radioButtonParitySpace.Checked) serialPortarduino.Parity = Parity.Space;

                    if (radioButtonStopbitsNone.Checked) serialPortarduino.StopBits = StopBits.One;
                    else if (radioButtonStopbitsOne.Checked) serialPortarduino.StopBits = StopBits.One;
                    else if (radioButtonStopbitsOnePointFive.Checked) serialPortarduino.StopBits = StopBits.OnePointFive;
                    else if (radioButtonStopbitsTwo.Checked) serialPortarduino.StopBits = StopBits.Two;

                    if (radioButtonHandshakeNone.Checked) serialPortarduino.Handshake = Handshake.None;
                    else if (radioButtonHandshakeRTS.Checked) serialPortarduino.Handshake = Handshake.RequestToSend;
                    else if (radioButtonHandshakeRTSXonXoff.Checked) serialPortarduino.Handshake = Handshake.RequestToSendXOnXOff;
                    else if (radioButtonHandshakeXonXoff.Checked) serialPortarduino.Handshake = Handshake.XOnXOff;

                    serialPortarduino.RtsEnable = true; // Forceer op true voor Leonardo
                    serialPortarduino.DtrEnable = true; // Forceer op true voor Leonardo

                    serialPortarduino.Open();
                    System.Threading.Thread.Sleep(2000); // Iets langer wachten voor Leonardo

                    // --- NIEUW: Maak de buffer leeg ---
                    serialPortarduino.DiscardInBuffer();

                    string commando = "ping";
                    serialPortarduino.WriteLine(commando);

                    string antwoord = serialPortarduino.ReadLine();
                    antwoord = antwoord.TrimEnd();
                    if (antwoord == "pong")
                    {
                        radioButtonVerbonden.Checked = true;
                        buttonConnect.Text = "Disconnect";
                        labelStatus.Text = "status : connected";
                        timerOefening5.Start();
                    }
                    else
                    {
                        serialPortarduino.Close();
                        labelStatus.Text = "error: verkeerd antwoord";
                    }
                }
            }
            catch (Exception exeption)
            {
                labelStatus.Text = "error: " + exeption.Message;
                serialPortarduino.Close();
                radioButtonVerbonden.Checked = false;
                buttonConnect.Text = "Connect";
            }
        }

        private void timerOefening5_Tick_1(object sender, EventArgs e)
        {
            try
            {
                if (serialPortarduino.IsOpen)
                {
                    // --- STAP 1: LEES POTENTIOMETER (A0) ---
                    serialPortarduino.DiscardInBuffer(); // Maak de lijn leeg
                    serialPortarduino.WriteLine("get a0");
                    System.Threading.Thread.Sleep(50); // Geef Arduino tijd om te antwoorden
                    string antwoordPot = serialPortarduino.ReadLine();
                    // Split op de dubbele punt en pak het laatste deel (het getal)
                    string getalPot = antwoordPot.Split(':').Last().Trim();
                    double rawPot = Convert.ToDouble(getalPot);

                    double gewensteTemp = (rawPot * (40.0 / 1023.0)) + 5.0;
                    labelGewensteTemp.Text = $"{Math.Round(gewensteTemp, 1)} °C";

                    // --- STAP 2: LEES SENSOR / BRUGGETJE (A1) ---
                    serialPortarduino.DiscardInBuffer(); // Maak de lijn weer leeg voor de volgende vraag
                    serialPortarduino.WriteLine("get a1");
                    System.Threading.Thread.Sleep(50);
                    string antwoordTemp = serialPortarduino.ReadLine();
                    string getalTemp = antwoordTemp.Split(':').Last().Trim();
                    double rawTemp = Convert.ToDouble(getalTemp);

                    double huidigeTemp = rawTemp * (500.0 / 1023.0);
                    labelHuidigeTemp.Text = $"{Math.Round(huidigeTemp, 1)} °C";

                    // --- STAP 3: LED LOGICA ---
                    if (huidigeTemp < gewensteTemp) serialPortarduino.WriteLine("set d2 1");
                    else serialPortarduino.WriteLine("set d2 0");
                }
            }
            catch (Exception) { /* Foutafhandeling */ }
        }

        }
    }



