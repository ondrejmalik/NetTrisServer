using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

string portStr;
int portInt = 8543;
do
{
    Console.WriteLine("Enter port");
    portStr = Console.ReadLine();
    if (portStr == "")
        break;
} while (!int.TryParse(portStr, out portInt));

Console.WriteLine("Server is Running..");
HashSet<(int index, UdpClient client, IPEndPoint ipEndPoint)> clients = new();
object _EnumerationLock = new();
int index = 0;
while (true)
{
//--accepting client
    UdpClient client = new UdpClient();
    client.Client.Bind(new IPEndPoint(IPAddress.Any, portInt));
//--

    var ipEndPoint = new IPEndPoint(IPAddress.Any, portInt);

    var data = client.Receive(ref ipEndPoint);
    client.Close();
    (int index, UdpClient client, IPEndPoint ipEndPoint) newClient = new();
    Console.WriteLine($"New Client Connected - {ipEndPoint.Address.ToString()} : {ipEndPoint.Port}");
    Console.WriteLine("Current Clients: " + clients.Count); // TODO: need to remove disconnected clients
    newClient.index = index;
    index++;
    newClient.client = new UdpClient();
    newClient.ipEndPoint = ipEndPoint;
    string message = "OK";
    data = Encoding.ASCII.GetBytes(message);
    for (int i = 0; i < 5; i++)
    {
        newClient.client.Send(data, data.Length, newClient.ipEndPoint);
        Console.WriteLine(
            $"Sending OK to {ipEndPoint.Address.ToString()} : {ipEndPoint.Port} from {newClient.client.Client.LocalEndPoint}");
        Thread.Sleep(32);
    }

    lock (_EnumerationLock)
    {
        clients.Add(newClient);
        if (index % 2 == 1)
        {
            Thread thread = new Thread(() => SendLoop(index - 1, index));
            thread.Start();
        }
    }
}

void SendLoop(int player1Index, int player2Index)
{
    (int index, UdpClient client, IPEndPoint ipEndPoint) valueTuple =
        clients.Where(item => item.index == player1Index).First();
    IEnumerable<(int index, UdpClient client, IPEndPoint ipEndPoint)> valueTuple2;
    Stopwatch s = new Stopwatch();
    byte[] recieved1;
    byte[] result2;
    while (true)
    {
        lock (_EnumerationLock)
        {
            // wait for the second player to connect
            valueTuple2 = clients.Where(item => item.index == player2Index);
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

            valueTuple2 = clients.Where(item => item.index == player2Index);


            var client2 = valueTuple2.First();
            result2 = client2.client.Receive(ref client2.ipEndPoint);
            //Console.WriteLine(Encoding.ASCII.GetString(result2)); //-- this is the message from the client2
            client2.client.Send(recieved1, recieved1.Length, client2.ipEndPoint);

            valueTuple.client.Send(result2, result2.Length, valueTuple.ipEndPoint);
            Console.WriteLine(s.ElapsedMilliseconds + "ms");
            s.Restart();
        }
    }
    catch (Exception e)
    {
        Console.WriteLine($"Clients {player1Index} and {player2Index} Disconnected");
    }
}