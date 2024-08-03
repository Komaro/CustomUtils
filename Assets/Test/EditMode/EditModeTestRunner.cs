using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public class EditModeTestRunner {
    
    // [TestCase(1)]
    // [TestCase(2)]
    // public void CaseTest(int value) {
    //     Logger.TraceLog($"{nameof(CaseTest)} param || {value}");
    // }

    private static SimpleTcpServer _server;
    

    [Test]
    public async Task TcpServerTest() {
        var server = new SimpleTcpServer();
        server.Start();

        using (var client = new TcpClient("localhost", 8890)) {
            if (client.Connected == false) {
                Logger.TraceError("Connect Failed");
                return;
            }

            await using (var stream = client.GetStream()) {
                var bytes = "Send Test".GetBytes();
                await stream.WriteAsync(BitConverter.GetBytes(bytes.Length));
                await stream.WriteAsync(bytes);
            }
        }

        server.Stop();
    }
}
