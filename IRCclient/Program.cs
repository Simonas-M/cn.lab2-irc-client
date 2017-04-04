using System;

namespace IRCclient
{
    class Program
    {
        static void Main(string[] args)
        {
            int port;
            string nick, user, server, channel;
            GetUserInput(out nick, out user, out server, out port, out channel);

            IRCclient client = new IRCclient(server, port, nick, user, channel);
            if(client.Connect())
            {
                client.StartReadingTask();
                while(true)
                {
                    try
                    {
                        client.Write(Console.ReadLine());
                    }
                    catch(Exception)
                    {
                        break;
                    }
                }
                Console.WriteLine("Your connection to the server was terminated\nClosing application in 5 seconds");
                System.Threading.Thread.Sleep(5000);
            }

        }

        static void GetUserInput(out string nick, out string user, out string server, out int port, out string channel)
        {
            Console.Write("Enter bot nick: ");
            nick = "clientukas";
            //Console.ReadLine();

            Console.Write("Enter bot owner name: ");
            user = "SIMUKAS";
            //Console.ReadLine();

            Console.Write("Enter server name: ");
            server = "chat.freenode.net";
            //Console.ReadLine();

            Console.Write("Enter port number: ");
            port = 6667;
            //Convert.ToInt32(Console.ReadLine());

            Console.Write("Channel: ");
            channel = "#haxtest";
            //Console.ReadLine();
        }
    }
}
