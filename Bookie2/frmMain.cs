using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sulakore.Communication;
using Sulakore.Habbo;
using Sulakore.Modules;
using Sulakore.Protocol;
using Tangine;
using WebSocketSharp;

namespace Bookie2
{
    [Module("Bookie2", "Casinos, bitches.")]
    [Author("Scott Stamp", HabboName = "Iterator", Hotel = HHotel.Com)]
    public partial class FrmMain : ExtensionForm
    {
        private WebSocket _socket;
        private int _roomId = 0; 

        public FrmMain()
        {
            InitializeComponent();
        }

        private void FrmMain_Load(object sender, EventArgs e)
        {
            Triggers.OutAttach(1912, DiceRolled);
            Triggers.OutAttach(2960, DiceClosed);
            Triggers.OutAttach(3718, EnterRoom);
            Triggers.InAttach(2726, FurniLoaded);
            InitializeSocket();
        }

        private void DiceRolled(DataInterceptedEventArgs e)
        {
            _socket.Send($"roll:{e.Packet.ReadInteger()}");
            e.IsBlocked = true;
        }

        private void DiceClosed(DataInterceptedEventArgs e)
        {
            _socket.Send($"close:{e.Packet.ReadInteger()}");
            e.IsBlocked = true;
        }

        private void EnterRoom(DataInterceptedEventArgs e)
        {
            _roomId = e.Packet.ReadInteger();
            _socket.Send($"loadedroom:{_roomId}");
        }

        private void FurniLoaded(DataInterceptedEventArgs e)
        {
            if (_roomId == 72555667)
                e.IsBlocked = true;

            _socket.Send($"loadedroom:{_roomId}");
        }

        private void InitializeSocket()
        {
            _socket = new WebSocket("ws://192.99.109.121:3000/");
            _socket.OnOpen += Socket_OnOpen;
            _socket.OnClose += Socket_OnClose;
            _socket.OnMessage += Socket_OnMessage;
            _socket.Connect();
        }

        private void Socket_OnClose(object sender, CloseEventArgs e)
        {
            ssMain.Invoke((MethodInvoker)delegate
            {
                lblStatus.Text = "Status: Disconnected";
            });
        }

        private void Socket_OnOpen(object sender, EventArgs e)
        {
            ssMain.Invoke((MethodInvoker)delegate
            {
                lblStatus.Text = "Status: Connected";
            });
        }

        private void Socket_OnMessage(object sender, MessageEventArgs e)
        {
            var parameters = e.Data.Split(':');
            switch (parameters[0])
            {
                case "update":
                    var id = int.Parse(parameters[1]);
                    var result = int.Parse(parameters[2]);
                    Connection.SendToClientAsync(934, id, result);
                    break;
                case "furni":
                    if (_roomId == 72555667)
                    {
                        var msg = new HMessage(StringToByteArray(parameters[1].Replace("01D4", "0AA6")));
                        Connection.SendToClientAsync(msg.ToBytes());
                    }
                    break;
            }
        }

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
    }
}
