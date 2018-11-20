using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using MySql.Data;
using MySql.Data.MySqlClient;

namespace tester
{
    class Program
    {
        static void Main(string[] args)
        {

            //Database details
            MySqlConnection connect = new MySqlConnection("server="";user id="";password="";database=""");
            SerialPort SP = new SerialPort();
            int occupied = 0;
            int nodeID = 0;
            int sensorID = 0;
            int spaceStatus = 0;
            int check = 0;
            MySqlCommand command;
            DateTime arrival;
            DateTime departure;
            SP.BaudRate = 9600;

            Console.Write("Enter COM for arduino: ");
            string com = Console.ReadLine();
            SP.PortName = com;
            SP.RtsEnable = true;
            SP.DtrEnable = true;
            SP.Open();

            if(SP.IsOpen)
            {
                Console.WriteLine("Port open");
            }

            try
            {
                connect.Open();
            }
            catch (Exception excep)
            {
                Console.Write(excep.Message);
            }

            while (true)
            {
                nodeID = Convert.ToInt16(SP.ReadLine());
                sensorID = Convert.ToInt16(SP.ReadLine());
                spaceStatus = Convert.ToInt16(SP.ReadLine());

                Console.WriteLine(nodeID);
                Console.WriteLine(sensorID);
                Console.WriteLine(spaceStatus);
                Console.WriteLine();

                //Sensor has detected that a space is unoccupied.
                if (spaceStatus == 0)
                {
                    //Need to check if that carpark space in the DB is occupied.
                    command = connect.CreateCommand();
                    command.CommandText = "SELECT occupied FROM Spaces WHERE spaceID=(?spaceID)";
                    command.Parameters.AddWithValue("?spaceID", sensorID);
                    MySqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        check = reader.GetInt16("occupied");
                    }

                    reader.Close();

                    /*If check is equal to 1, then that space was occupied and has now become unoccupied.
                     * We need to record the departure datetime for that visit*/
                    if (check != 0)
                    {
                        command.Connection.CreateCommand();
                        departure = DateTime.Now;
                        command.CommandText = "UPDATE Visits SET departure=(?departure) WHERE spaceID=(?theid) ORDER BY arrival desc LIMIT 1";
                        command.Parameters.AddWithValue("?departure", departure);
                        command.Parameters.AddWithValue("?theID", sensorID);
                        command.ExecuteNonQuery();
                    }

                    //Set that space to unoccupied in the DB
                    command = connect.CreateCommand();
                    command.CommandText = "UPDATE Spaces SET occupied=0 WHERE spaceID=(?id)";
                    command.Parameters.AddWithValue("?id", sensorID);
                    command.ExecuteNonQuery();
                }

                //Sensor has detected space is occupied
                else if (spaceStatus == 1)
                {
                    //Need to check if that carpark space in the DB is occupied.
                    command = connect.CreateCommand();
                    command.CommandText = "SELECT occupied FROM Spaces WHERE spaceID=(?spaceID)";
                    command.Parameters.AddWithValue("?spaceID", sensorID);
                    MySqlDataReader reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        check = reader.GetInt16("occupied");
                    }

                    reader.Close();

                    /*If check is equal to 1, then that space is already occupied. So we do nothing.*/
                    if (check == 0)
                    {
                        command = connect.CreateCommand();
                        command.CommandText = "UPDATE Spaces SET occupied=1 WHERE spaceID=(?id)";
                        command.Parameters.AddWithValue("?id", sensorID);
                        command.ExecuteNonQuery();

                        //Record the visit in DB.
                        command.Connection.CreateCommand();
                        arrival = DateTime.Now;
                        command.CommandText = "INSERT INTO Visits (arrival, spaceID) VALUES (?arrival, ?spaceID)";
                        command.Parameters.AddWithValue("?arrival", arrival);
                        command.Parameters.AddWithValue("?spaceID", sensorID);
                        command.ExecuteNonQuery();
                        Console.WriteLine("Arrived");
                    }
                  
                }

            }
        }


    }

}
    

