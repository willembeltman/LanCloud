using LanCloud.Domain.Application;
using System;
using System.Linq;
using System.Windows.Forms;

namespace LanCloud.Forms
{
    public partial class StatusForm : Form
    {
        public StatusForm(LocalApplication application)
        {
            Application = application;

            Application.OnStatusChanged += Application_OnStateChanged;
            FormClosing += StatusForm_FormClosing;

            InitializeComponent();
        }

        private void StatusForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            KillSwitch = true;
        }

        private void Application_OnStateChanged(object sender, EventArgs e)
        {
            if (!KillSwitch)
            {
                this.Invoke((MethodInvoker)delegate
                {
                    ShowStatus();
                });
            }
        }

        public LocalApplication Application { get; }
        public bool KillSwitch { get; private set; }

        private void StatusForm_Load(object sender, EventArgs e)
        {
            ShowStatus();
        }

        private void ShowStatus()
        {
            treeView1.Nodes.Clear();

            var applicationNode = new TreeNode($"Local Application");

            var name = "";
            if (Application.LocalApplicationServerConfig != null)
            {
                name = $"{Application.LocalApplicationServerConfig.HostName}:{Application.LocalApplicationServerConfig.Port} ";
            }

            var applicationInnerNode = new TreeNode($"{Application.HostName} {name}{Application.Status}");
            applicationNode.Nodes.Add(applicationInnerNode);

            var connections2 = Application.LocalApplicationServer?.GetActiveConnections();
            if (connections2?.Any() == true)
            {
                var connectionsNode = new TreeNode("Connections");
                foreach (var connection in connections2)
                {
                    // Tweede laag onder Child 1
                    connectionsNode.Nodes.Add(new TreeNode($"{connection.Name} {connection.Status}"));
                }
                applicationInnerNode.Nodes.Add(connectionsNode);
            }

            treeView1.Nodes.Add(applicationNode);

            if (Application.LocalShareStripes?.Any() == true)
            {
                var localSharesNode = new TreeNode("Local Shares");
                foreach (var localShare in Application.LocalShares)
                {
                    var name2 = localShare.Server == null ?
                        " " : $" {localShare.HostName}:{localShare.Port} ";
                    var localShareNode = new TreeNode($"{localShare.Root.FullName}{name2} {localShare.Status}");

                    var connections = localShare.Server?.GetActiveConnections();
                    if (connections?.Any() == true)
                    {
                        var connectionsNode = new TreeNode("Connections");
                        foreach (var connection in connections)
                        {
                            // Tweede laag onder Child 1
                            connectionsNode.Nodes.Add(new TreeNode($"{connection.Name}: {connection.Status}"));
                        }
                        localShareNode.Nodes.Add(connectionsNode);
                    }

                    localSharesNode.Nodes.Add(localShareNode);
                }
                applicationNode.Nodes.Add(localSharesNode);
            }

            if (Application.RemoteApplications?.Any() == true)
            {
                var remoteApplicationsNode = new TreeNode("Remote Applications");
                foreach (var remoteApplication in Application.RemoteApplications)
                {
                    var remoteApplicationNode = new TreeNode($"{remoteApplication.HostName}:{remoteApplication.Port} {remoteApplication.Status}");

                    if (remoteApplication.RemoteShares.Any())
                    {
                        var remoteSharesNode = new TreeNode("Remote Shares");
                        foreach (var remoteShare in remoteApplication.RemoteShares)
                        {
                            // Tweede laag onder Child 1
                            remoteSharesNode.Nodes.Add(new TreeNode($"{remoteShare.HostName}:{remoteShare.Port} {remoteShare.Status}"));
                        }
                        remoteApplicationNode.Nodes.Add(remoteSharesNode);
                    }

                    remoteApplicationsNode.Nodes.Add(remoteApplicationNode);
                }
                treeView1.Nodes.Add(remoteApplicationsNode);
            }

            var virtualFtpServerNode = new TreeNode($"Virtual Ftp Server");
            virtualFtpServerNode.Nodes.Add(new TreeNode($"{Application.VirtualFtpServer.HostName}:{Application.VirtualFtpServer.Port} {Application.VirtualFtpServer.FtpServer.Status}"));
            treeView1.Nodes.Add(virtualFtpServerNode);

            //var connections3 = Application.VirtualFtpServer.FtpServer.GetActiveConnections();
            //if (connections3.Any())
            //{
            //    var connectionsNode = new TreeNode("Connections");
            //    foreach (var connection in connections3)
            //    {
            //        // Tweede laag onder Child 1
            //        connectionsNode.Nodes.Add(new TreeNode($"{connection.Name}: {connection.Status}"));
            //    }
            //    applicationInnerNode.Nodes.Add(connectionsNode);
            //}


            // Open de boomstructuur
            treeView1.ExpandAll();

        }
        public void Kill()
        {
            KillSwitch = true;
            this.Close();
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            Application.Logger.LogInfo = checkBox1.Checked;
        }
    }
}
