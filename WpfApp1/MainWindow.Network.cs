using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using iNKORE.UI.WPF.Modern.Controls;
using userdata;
using OPL_WpfApp.Utils;
using MessageBox = iNKORE.UI.WPF.Modern.Controls.MessageBox;

namespace OPL_WpfApp
{
    /// <summary>
    /// 网络与隧道操作
    /// </summary>
    public partial class MainWindow_opl : Window
    {
        private void Opentun(object sender, RoutedEventArgs e)
        {
            if (tunbutton.Content.ToString() == "创建/开启网络" && on)
            {
                MessageBox.Show("程序在运行，禁止操作!", "警告");
                return;
            }
            if (!on && OpenDate.AddSeconds(1) > DateTime.Now && OpenDate != null)
            {
                MessageBox.Show("操作太频繁，请稍后再试 (请至少间隔 1s，防止出现 BUG)", "警告");
                return;
            }
            int port = 25674;
            string linkcode = $"2:{userData.UID}:{port}:{port}";
            if (tunbutton.Content.ToString() == "创建/开启网络")
            {
                tunjoinbutton.IsEnabled = false;
                port = int.Parse(tunport.Text.Replace(" ", ""));
                if (Copy_text(linkcode))
                    MessageBox.Show($"复制成功：连接码 {linkcode} \n请将该内容粘贴给要进行组网的人添加", "提示");
            }
            else
            {
                tunjoinbutton.IsEnabled = true;
            }
            tunnel.OpenTunnel(tunbutton, 1, port);
            iptext.Text = "10.0.23.1";
            
            sjson.getjson();
            sjson.Alloff();
            if (over) Strapp();
        }

        private void jointun(object sender, RoutedEventArgs e)
        {
            if (tunjoinbutton.Content.ToString() == "连接网络" && on)
            {
                MessageBox.Show("程序在运行，禁止操作!", "警告");
                return;
            }
            if (!on && OpenDate.AddSeconds(1) > DateTime.Now && OpenDate != null)
            {
                MessageBox.Show("操作太频繁，请稍后再试 (请至少间隔 1s，防止出现 BUG)", "警告");
                return;
            }
            string linkcode = tunlink.Text.Replace(" ", "");
            if (linkcode == "")
            {
                MessageBox.Show("请输入连接码", "提示");
                return;
            }
            int id = 2;
            int cport = 25674;
            if (tunjoinbutton.Content.ToString() == "连接网络")
            {
                tunbutton.IsEnabled = false;
                id = int.Parse(tunip.Text.Replace(" ", ""));
                cport = int.Parse(tunport.Text.Replace(" ", ""));
                var connections = ConnectionParser.ParseConnections(linkcode);
                sjson.getjson();
                sjson.Alloff();
                sjson.clearoindex();
                foreach (var conn in connections)
                {
                    string type = conn.Protocol;
                    if (type == "1") type = "tcp";
                    if (type == "2") type = "udp";
                    string uid = conn.UID;
                    int port = conn.Port;
                    cport = conn.CPort;
                    if (!sjson.Add1link(type, uid, port, cport)) return;
                }
                Relist();
            }
            else
            {
                tunbutton.IsEnabled = true;
            }
            tunnel.OpenTunnel(tunjoinbutton, id, cport);
            iptext.Text = $"10.0.23.{id}";
            if (over) Strapp();
        }

        private void copytunip(object sender, RoutedEventArgs e)
        {
            if (Copy_text(iptext.Text))
                MessageBox.Show($"复制成功： {iptext.Text} ", "提示");
        }

        private void crearEtNet(object sender, RoutedEventArgs e)
        {
            StartEasyTier(UID.Text);
        }

        private void joinEtNet(object sender, RoutedEventArgs e)
        {
            if (etNetText.Text == "")
            {
                MessageBox.Show("请输入连接码或创建房间", "提示");
                return;
            }
            StartEasyTier(etNetText.Text);
        }

        private void StartEasyTier(string networkName)
        {
            if (!TryNormalizeEasyTierPeers(string.Join(",", EasyTierPeersList.Items.Cast<string>()), out string peers))
            {
                MessageBox.Show("请至少添加一个有效的 tcp:// 或 udp:// 节点地址。", "提示");
                return;
            }
            ets?.setlinkname(networkName);
            newetuid.Text = networkName;
            ets?.Open(peers);
        }

        private void AddEasyTierPeer_Click(object sender, RoutedEventArgs e)
        {
            if (!TryNormalizeEasyTierPeers(EasyTierPeerInput.Text, out string peers))
            {
                MessageBox.Show("请输入有效的 tcp:// 或 udp:// 节点地址。", "提示");
                return;
            }
            foreach (string peer in peers.Split(','))
                if (!EasyTierPeersList.Items.Cast<string>().Any(item => string.Equals(item, peer, StringComparison.OrdinalIgnoreCase)))
                    EasyTierPeersList.Items.Add(peer);
            SaveEasyTierPeers();
            EasyTierPeerInput.Clear();
            EasyTierPeerInput.Focus();
        }

        private void RemoveEasyTierPeer_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string peer)
            {
                EasyTierPeersList.Items.Remove(peer);
                SaveEasyTierPeers();
            }
        }

        private void SaveEasyTierPeers()
        {
            set.settings.EasyTierPeers = EasyTierPeersList.Items.Cast<string>().ToList();
            set.Write();
        }

        private void EasyTierPeerInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            AddEasyTierPeer_Click(sender, e);
            e.Handled = true;
        }

        internal static bool TryNormalizeEasyTierPeers(string input, out string peers)
        {
            var nodes = new List<string>();
            foreach (string raw in (input ?? "").Split(new[] { ',', ';', '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries))
            {
                string node = raw.Trim().TrimEnd('/');
                if (!Uri.TryCreate(node, UriKind.Absolute, out Uri uri) ||
                    (uri.Scheme != "tcp" && uri.Scheme != "udp") ||
                    string.IsNullOrWhiteSpace(uri.Host) || uri.Port < 1 || uri.Port > 65535)
                {
                    peers = null;
                    return false;
                }
                if (!nodes.Exists(value => string.Equals(value, node, StringComparison.OrdinalIgnoreCase)))
                    nodes.Add(node);
            }
            peers = string.Join(",", nodes);
            return nodes.Count > 0;
        }

        private void leaveEtNet(object sender, RoutedEventArgs e)
        {
            ets?.Stop();
            newetuid.Text = "未连接";
        }

        private void EThelp(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start("https://blog.gldhn.top/2026/07/02/opl_zw2/");
            }
            catch (Exception ex) { Logger.Log($"[错误]打开 EasyTier 帮助失败：{ex.Message}"); }
        }

        private void ServersCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            string server;
            if (ServersCombo.SelectedValue != null)
                server = ServersCombo.SelectedValue.ToString();
            else
                return;
            if(server == "System.Windows.Controls.ComboBoxItem: 获取 ing") return;
            server = server.Replace("System.Windows.Controls.ComboBoxItem: ", "");
            if(net.servers==null)net.getjson();
            try
            {
                foreach (var item in net.servers)
                {
                    if (item.ServerName == server)
                    {
                        if (item.ServerName != "主节点")
                        {
                            opname = "openp2p21.exe";
                            string filename = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", opname);
                            if (!File.Exists(filename))
                            {
                                new Updata(Net.Getmirror("https://file.gldhn.top/file/openp2p-r3.21.12.windows-386.zip"), "openp2p21.zip");
                            }
                        }
                        break;
                    }
                }
            }
            catch (Exception)
            {
            }
        }
    }
}
