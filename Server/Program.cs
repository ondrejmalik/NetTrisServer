using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;
Console.WriteLine("Server is Running..");
HashSet<(int index, UdpClient client, IPEndPoint ipEndPoint)> clients = new();
int index = 0;
while (true)
{
//--accepting client
    UdpClient client = new UdpClient();
    client.Client.Bind(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8543));
//--

    var ipEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 8543);

    var data = client.Receive(ref ipEndPoint);
    client.Close();
    (int index, UdpClient client, IPEndPoint ipEndPoint) newClient = new();
    Console.WriteLine($"New Client Connected - {ipEndPoint.Address.ToString()}");
    Console.WriteLine("Current Clients: " + clients.Count);
    newClient.index = index;
    index++;
    newClient.client = new UdpClient();
    newClient.ipEndPoint = ipEndPoint;
    string message = "OK";
    data = Encoding.ASCII.GetBytes(message);
    newClient.client.Send(data, data.Length, newClient.ipEndPoint);
    clients.Add(newClient);
    if (index % 2 == 1)
    {
        Thread thread = new Thread(() => SendLoop(index - 1, index));
        thread.Start();
    }
}

void SendLoop(int player1Index, int player2Index)
{
    while (true)
    {
        Stopwatch s = new Stopwatch();
        s.Start();
        byte[] recieved1;

        var valueTuple = clients.Where(item => item.index == player1Index).First();
        recieved1 = valueTuple.client.Receive(ref valueTuple.ipEndPoint);
        //Console.WriteLine(Encoding.ASCII.GetString(recieved1)); //-- this is the message from the client1

        var valueTuple2 = clients.Where(item => item.index == player2Index);
        byte[] result2 = new byte[0];
        if (valueTuple2.Count() > 0)
        {
            var client2 = valueTuple2.First();
            result2 = client2.client.Receive(ref client2.ipEndPoint);
            //Console.WriteLine(Encoding.ASCII.GetString(result2)); //-- this is the message from the client2
            client2.client.Send(recieved1, recieved1.Length, client2.ipEndPoint);
        }

        valueTuple.client.Send(result2, result2.Length, valueTuple.ipEndPoint);
        s.Stop();
        Console.WriteLine(s.ElapsedMilliseconds);
    }
}