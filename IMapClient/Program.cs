﻿using System;
using System.Text;

//https://code.msdn.microsoft.com/windowsdesktop/Simple-IMAP-CLIENT-b249d2e6

namespace IMapClient
{
    class Program
    {
        static System.IO.StreamWriter sw = null;
        static System.Net.Sockets.TcpClient tcpc = null;
        static System.Net.Security.SslStream ssl = null;
        static string username, password;
        static string path;
        static int bytes = -1;
        static byte[] buffer;
        static StringBuilder sb = new StringBuilder();
        static byte[] dummy;
        static void Main(string[] args)
        {
            try
            {
                path = Environment.CurrentDirectory + "\\emailresponse.txt";

                if (System.IO.File.Exists(path))
                    System.IO.File.Delete(path);

                sw = new System.IO.StreamWriter(System.IO.File.Create(path));
                // there should be no gap between the imap command and the \r\n       
                // ssl.read() -- while ssl.readbyte!= eof does not work because there is no eof from server 
                // cannot check for \r\n because in case of larger response from server ex:read email message 
                // there are lot of lines so \r \n appears at the end of each line 
                //ssl.timeout sets the underlying tcp connections timeout if the read or write 
                //time out exceeds then the undelying connection is closed 
                tcpc = new System.Net.Sockets.TcpClient("imap.gmail.com", 993);

                ssl = new System.Net.Security.SslStream(tcpc.GetStream());
                ssl.AuthenticateAsClient("imap.gmail.com");
                receiveResponse("");
                receiveResponse("$ CAPABILITY\r\n");
                Console.WriteLine("username : ");
                username = Console.ReadLine();
                Console.WriteLine("password : ");
                password = Console.ReadLine();
                receiveResponse("$ LOGIN " + username + " " + password + "\r\n");
                Console.Clear();

                receiveResponse("$ LIST " + "\"\"" + " \"*\"" + "\r\n");
                receiveResponse("$ SELECT INBOX\r\n");
                receiveResponse("$ STATUS INBOX (MESSAGES)\r\n");

                Console.WriteLine("enter the email number to fetch :");
                int number = int.Parse(Console.ReadLine());

                fetchEmailHeader(number);
                fetchEmailBody(number);
                Console.WriteLine(receiveResponse("$ LOGOUT\r\n"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("error: " + ex.Message);
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                    sw.Dispose();
                }
                if (ssl != null)
                {
                    ssl.Close();
                    ssl.Dispose();
                }
                if (tcpc != null)
                {
                    tcpc.Close();
                }
            }
            Console.ReadKey();
        }
        private static string fetchEmailHeader(int number)
        {
            string command = "$ FETCH " + number + " body[header]\r\n";
            string ret = receiveResponse(command);
            string header = ret;
            while (!ret.EndsWith("$ OK Success\r\n"))
            {
                ret = receiveResponse("");
                ret = ret.Trim('\0');
                header = header + ret;
            }
            return header;
        }
        private static string fetchEmailBody(int number)
        {
            string command = "$ FETCH " + number + " body.peek[text]\r\n";
            string ret = receiveResponse(command);
            string body = ret;
            while (!ret.EndsWith("$ OK Success\r\n"))
            {
                ret = receiveResponse("");
                ret = ret.Trim('\0');
                body = body + ret;
            }
            return body;
        }

        static string receiveResponse(string command)
        {
            try
            {
                if (command != "")
                {
                    if (tcpc.Connected)
                    {
                        dummy = Encoding.ASCII.GetBytes(command);
                        //dummy = Encoding.UTF8.GetBytes(command);
                        //Console.WriteLine(Encoding.UTF8.GetString(dummy));
                        ssl.Write(dummy, 0, dummy.Length);
                    }
                    else
                    {
                        throw new ApplicationException("TCP CONNECTION DISCONNECTED");
                    }
                }
                ssl.Flush();


                buffer = new byte[2048];
                bytes = ssl.Read(buffer, 0, 2048);
                //sb.Append(Encoding.ASCII.GetString(buffer));

                //Console.WriteLine(sb.ToString());
                //sw.WriteLine(sb.ToString());
                //sb = new StringBuilder();
                Console.WriteLine(Encoding.ASCII.GetString(buffer));
                return Encoding.ASCII.GetString(buffer);
            }
            catch (Exception ex)
            {
                throw new ApplicationException(ex.Message);
            }
        }

    }
}
