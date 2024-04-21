using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class UdpGameServer
{
    public string portStr { get; set; }
    private int Port { get; set; } = 8543;
    public HashSet<(int index, UdpClient client, IPEndPoint ipEndPoint)> Clients = new();
    private object _enumerationLock = new();
    private int _index = 0;
    private int _disconnectedClients = 0;
    private Action _msPrint;
    public int CurrentCientCount => Clients.Count - _disconnectedClients;

    public void Start()
    {
        SetPort();
        SetMsPrint();
        FindClients();
    }

    private void SetPort()
    {
        int result = 0;
        do
        {
            Console.WriteLine("Enter port");
            portStr = Console.ReadLine();
            if (portStr == "")
                break;
        } while (!int.TryParse(portStr, out result));

        if (result != 0)
        {
            Port = result;
        }
    }

    private void SetMsPrint()
    {
        bool result = false;
        do
        {
            Console.WriteLine("Print ms? (true/false)");
            portStr = Console.ReadLine();
            if (portStr == "")
                break;
        } while (!bool.TryParse(portStr, out result));

        if (result)
        {
            _msPrint = () => { Console.WriteLine("ms"); };
        }
        else
        {
            _msPrint = () => { };
        }
    }


    private void FindClients()
    {
        Console.WriteLine("Server is Running..");
        while (true)
        {
            UdpClient client = new UdpClient();
            client.Client.Bind(new IPEndPoint(IPAddress.Any, Port));

            var ipEndPoint = new IPEndPoint(IPAddress.Any, Port);

            var data = client.Receive(ref ipEndPoint);
            client.Close();
            (int index, UdpClient client, IPEndPoint ipEndPoint) newClient = new();
            Console.WriteLine($"New Client Connected - {ipEndPoint.Address.ToString()} : {ipEndPoint.Port}");
            newClient.index = _index;
            _index++;
            newClient.client = new UdpClient();
            newClient.ipEndPoint = ipEndPoint;
            string message = "OK";
            data = Encoding.ASCII.GetBytes(message);
            for (int i = 0; i < 5; i++) // send 5 attempts to make sure the client recieves the message
            {
                newClient.client.Send(data, data.Length, newClient.ipEndPoint);
                newClient.client.Client.ReceiveTimeout = 5000;
                Console.WriteLine(
                    $"Sending OK to {ipEndPoint.Address.ToString()} : {ipEndPoint.Port} from {newClient.client.Client.LocalEndPoint}");
                Thread.Sleep(32);
            }

            lock (_enumerationLock)
            {
                Clients.Add(newClient);
                if (_index % 2 == 1)
                {
                    Thread thread = new Thread(() => SendLoop(_index - 1, _index));
                    thread.Start();
                }
            }

            Console.WriteLine($"Current Clients: {CurrentCientCount}");
        }
    }

    private void SendLoop(int player1Index, int player2Index)
    {
        (int index, UdpClient client, IPEndPoint ipEndPoint) valueTuple =
            Clients.Where(item => item.index == player1Index).First();
        IEnumerable<(int index, UdpClient client, IPEndPoint ipEndPoint)> valueTuple2;
        Stopwatch s = new Stopwatch();
        byte[] recieved1;
        byte[] result2;
        while (true)
        {
            lock (_enumerationLock)
            {
                // wait for the second player to connect
                valueTuple2 = Clients.Where(item => item.index == player2Index);
                if (valueTuple2.Count() > 0)
                {
                    Console.WriteLine("Both Players Connected");
                    var client2 = valueTuple2.First();
                    byte[] readyMessage = Encoding.ASCII.GetBytes("GameReady");
                    // TODO: add makin sure the readyMessages are recieved 
                    client2.client.Send(readyMessage, readyMessage.Length, client2.ipEndPoint);
                    valueTuple.client.Send(readyMessage, readyMessage.Length, valueTuple.ipEndPoint);
                    Console.WriteLine("Game Ready Sent");
                    break;
                }
            }
        }

        s.Start();
        try
        {
            while (true)
            {
                recieved1 = valueTuple.client.Receive(ref valueTuple.ipEndPoint);
                //Console.WriteLine(Encoding.ASCII.GetString(recieved1)); //-- this is the message from the client1

                valueTuple2 = Clients.Where(item => item.index == player2Index);


                var client2 = valueTuple2.First();
                result2 = client2.client.Receive(ref client2.ipEndPoint);
                //Console.WriteLine(Encoding.ASCII.GetString(result2)); //-- this is the message from the client2
                client2.client.Send(recieved1, recieved1.Length, client2.ipEndPoint);

                valueTuple.client.Send(result2, result2.Length, valueTuple.ipEndPoint);
                _msPrint();
                s.Restart();
            }
        }
        catch (Exception e)
        {
            _disconnectedClients += 2;
            Console.WriteLine($"Clients {player1Index} and {player2Index} Disconnected");
        }
    }
}