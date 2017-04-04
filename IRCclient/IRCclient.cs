using System;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Collections.Generic;

namespace IRCclient
{
    class IRCclient
    {
        int port;
        string server;
        TcpClient socket = new TcpClient();
        TextReader input;
        TextWriter output;

        string nick;
        string user;
        List<string> channel = new List<string>();
        int chnNo = 0;
        string buf = "";
        ConsoleColor cColor = ConsoleColor.Magenta;

        public IRCclient(string server, int port, string nick, string user, string channel)
        {
            this.server = server;
            this.port = port;
            this.nick = nick;
            this.user = user;
            this.channel.Add(channel);
        }

        public bool Connect()
        {
            try
            {
                Console.WriteLine("Trying to connect...");
                socket.ConnectAsync(server, port);
                for (int i = 0; i < 10; i++)
                    if (!socket.Connected)
                        Thread.Sleep(1000);
                if (socket.Connected)
                {
                    input = new StreamReader(socket.GetStream());
                    output = new StreamWriter(socket.GetStream());
                    Write("/USER " + nick + " 0 * :" + user + "\r\n");
                    Write("/NICK " + nick + "\r\n");
                    return true;
                }
                Console.WriteLine("\nConnection timeout");
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("\nAn error was encountered!\nPlease try again");
                return false;
            }
        }

        public bool StartReadingTask()
        {
            if (!socket.Connected)
                return false;
            new Task(() =>
            {
                while (true)
                {
                    buf = input.ReadLineAsync().Result;
                    if (buf[0] != ':')
                        continue;
                    if (buf.StartsWith("PING "))
                    {
                        output.Write('/' + buf.Replace("PING", "PONG") + "\r\n");
                        output.Flush();
                    }
                    if (buf.Split(' ')[1] == "001")
                    {
                        output.Write(
                            "MODE " + nick + " +B\r\n" +
                            "JOIN " + GetCurrentChannel() +"\r\n"
                        );
                        output.Flush();
                    }
                    if (CheckForUserInfoChange(buf))
                        continue;
                    buf = TidyUpServerMessage(buf);
                    Console.ForegroundColor = cColor;
                    Console.WriteLine(buf);
                }
            }).Start();//Skaito IRC serverio pranesimus
            return true;
        }

        public bool Write(string command)
        {
            if (socket.Connected)
            {
                if (Console.CursorTop != 0)
                    Console.CursorTop--;
                if (command.ToLower() == "/channel")
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine("Select channel number and press enter");
                    for (int i = 0; i < channel.Count; i++)
                    {
                        Console.WriteLine(i + " " + channel[i]);
                    }
                    while(true)
                    {
                        try
                        {
                            chnNo = Convert.ToInt32(Console.ReadLine());
                            if (chnNo < 0 || chnNo > channel.Count - 1)
                                throw new ArgumentException();
                            break;
                        }
                        catch (Exception)
                        {
                            Console.WriteLine("Please enter a valid number");
                        }
                    }
                    Console.CursorTop--;
                    Console.WriteLine("You switched to " + GetCurrentChannel());
                    return true;
                }
                    
                if (!command.StartsWith("/"))//Jei rasoma zinute i chata
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(GetDate() +'[' + GetCurrentChannel() + ']' + nick + ":" + command);
                    command = "PRIVMSG " + GetCurrentChannel() + " :" + command;
                }
                else//Jei rasoma komanda
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    command = command.Substring(1);
                    Console.WriteLine(GetDate() + "/" + command);
                }

                output.Write(command + "\r\n");
                output.Flush();
                return true;
            }
            return false;
        }

        private string GetDate()
        {
            return "[" + DateTime.Now.ToShortTimeString() + "]";
        }

        private string GetNickFromBuffer(string buf)
        {
            return buf.Substring(1, buf.IndexOf('!') - 1);
        }

        private string GetCurrentChannel()
        {
            if(chnNo < channel.Count-1)
                return channel[chnNo];
            return channel[channel.Count - 1];
        }

        private string TidyUpServerMessage(string text)
        {
            string[] nodes = text.Split(' ');
            string command = nodes[1];
            switch (command)
            {
                case "PRIVMSG":
                    text = text.Substring(1, text.IndexOf('!') - 1) + text.Substring(text.IndexOf(":", 1), text.Length - text.IndexOf(":", 1));
                    break;
                default:
                    if (text.Contains(" :"))
                        text = text.Substring(text.IndexOf(" :"), text.Length - text.IndexOf(" :"));
                    break;
            }
            if(nodes[2] == nick)
            {
                cColor = ConsoleColor.Magenta;
                return GetDate() + "[PM]" + text;
            }
            cColor = ConsoleColor.Yellow;
            return GetDate() + '[' + nodes[2] + ']' + text;
        }

        private bool CheckForUserInfoChange(string buffer)
        {
            switch(buffer.Split(' ')[1])
            {
                case "NICK":
                    Console.WriteLine(GetDate() + GetNickFromBuffer(buffer) + " has changed nick to " + buffer.Split(' ')[2].Substring(1));
                    if(GetNickFromBuffer(buffer) == nick)
                        nick = buffer.Split(' ')[2].Substring(1);
                    return true;
                case "JOIN":
                    Console.WriteLine(GetDate() + GetNickFromBuffer(buffer) + " joined #" + buffer.Split(' ')[2].Substring(1));
                    if (GetNickFromBuffer(buffer) == nick)
                        if(!channel.Contains('#' + buffer.Split(' ')[2].Substring(1)))
                            channel.Add('#' + buffer.Split(' ')[2].Substring(1));
                    return true;
                case "PART":
                    Console.WriteLine(GetDate() + GetNickFromBuffer(buffer) + " left " + buffer.Split(' ')[2].Substring(1));
                    if (GetNickFromBuffer(buffer) == nick)
                        buffer.Remove(buffer.IndexOf(buffer.Split(' ')[2].Substring(1)));
                    return true;
                case "KICK":
                    Console.WriteLine(GetDate() + GetNickFromBuffer(buffer) + " were kicked from " + buffer.Split(' ')[2]);
                    if(GetNickFromBuffer(buffer) == nick)
                        channel.Remove(buffer.Split(' ')[2]);
                    return true;
                case "INVITE":
                    Console.WriteLine(GetDate() + buffer.Substring(1, buffer.IndexOf('!') - 1) + " invited you to " + buffer.Split(' ')[3].Substring(1));
                    return true;
                case "QUIT":
                    Console.WriteLine(GetDate() + buffer.Substring(1, buffer.IndexOf('!') - 1) + " has quit");
                    return true;
                default:
                    return false;
            }
        }
    }
}
