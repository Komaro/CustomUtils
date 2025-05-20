using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

// TODO. 개발 예정. 함께 다른 형태의 서버들을 통합해서 구조적으로 통일할 수 있는지 확인
public class SimpleWebsocketServer : IDisposable {

    public SimpleWebsocketServer() {
        
    }

    ~SimpleWebsocketServer() => Dispose();

    public void Dispose() {
        
    }
}
