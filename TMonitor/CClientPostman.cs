﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.IO;
using Newtonsoft.Json;

namespace TMonitor
{
    public delegate void SuccessDeliveryHandler();
    public delegate void ErrorDeliveryHandler(CMessageEnvelope pMessageEnvelope);

    public abstract class CPostalService
    {
        //string GetInformation();
        //string FreeInformation();
        public abstract void Send(string pCorrespondence, string pSignature);
        public abstract bool SendPing(string pCorrespondence, string pSignature);
        public abstract bool SendLogs(string pCorrespondence, string pSignature);
        //Ивенты
        public abstract void OnSuccessDelivery();
        public abstract void OnErrorDelivery();
    }
    //
    //Responsible for network stuf
    //
    public class CClientPostman : CPostalService
    {
        public string serverAddress;
        public string httpMethod;
        public string httpContentType;
        //Не реализована сериализация-подержка следующих параметров в модуле - НАДО РЕАЛИЗОВАТЬ
        public string scheme = "http://";
        public uint port = 80;
        public uint requestTimeout = 5000;
        public uint maxRetryCount = 0;//колво попыток передать сообщение на сервер
        public uint tryDelay = 5000;//задержка между попытками в миллесекундах

        uint currentRetry = 0;
        CMessageEnvelope lastMessageEnvelope;
        //Конструктор
        public CClientPostman(){}
        public CClientPostman(string pServerAddress, string pMethod, string pContentType)
        {
            serverAddress = pServerAddress;
            httpMethod = pMethod;
            httpContentType = pContentType;
        }
        public bool Initialize(Dictionary<string, string> pConfiguration)
        {
            try
            {
                serverAddress = pConfiguration["serverAddress"];
                httpMethod = pConfiguration["httpMethod"];
                httpContentType = pConfiguration["httpContentType"];
                return true;
            }
            catch
            {
                return false;
            }
        }
        public override void Send(string pCorrespondence, string pSignature)
        {
            Console.WriteLine("This is try #" + (currentRetry + 1));
            try
            {
                CMessageEnvelope theEnvelope = new CMessageEnvelope(pCorrespondence, pSignature);
                lastMessageEnvelope = theEnvelope;
                WebRequest request = WebRequest.Create(string.Format("{0}{1}:{2}/", scheme, serverAddress, port));
                request.Method = httpMethod;
                request.Timeout = (int)requestTimeout;
                string postData = theEnvelope.ToJSON();
                byte[] byteArray = Encoding.UTF8.GetBytes(postData);
                request.ContentType = httpContentType;
                request.ContentLength = byteArray.Length;
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                WebResponse response = request.GetResponse();
                Console.WriteLine(((HttpWebResponse)response).StatusDescription);
                dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string responseFromServer = reader.ReadToEnd();
                Console.WriteLine(responseFromServer);
                reader.Close();
                dataStream.Close();
                response.Close();
            }
            catch(UriFormatException)
            {
                Console.WriteLine("UriFormatException");
            }
            catch(System.Net.WebException exception)
            {
                if (exception.Status == WebExceptionStatus.Timeout)
                    OnTimeout();
                else
                {
                    if (exception.Status == WebExceptionStatus.ConnectFailure)
                        OnConnectFailure();
                    else
                    {
                        if (exception.Status == WebExceptionStatus.ProtocolError)
                            OnProtocolError();
                        else
                        {
                            OnErrorDelivery();
                        }
                    }
                    
                }
            }
        }
        public override bool SendPing(string pCorrespondence, string pSignature)
        {
            Console.WriteLine("[tilli]: SendPing start");
            try
            {
                CMessageEnvelope theEnvelope = new CMessageEnvelope(pCorrespondence, pSignature);
                lastMessageEnvelope = theEnvelope;
                WebRequest request = WebRequest.Create(string.Format("{0}{1}:{2}/", scheme, serverAddress, port));
                Console.WriteLine("[tilli]: After creating WebRequest.Create");
                if(httpMethod == null)
                    Console.WriteLine("[tilli]: httpMethod is null ");
                Console.WriteLine("[tilli]: httpMethod " + httpMethod);
                request.Method = httpMethod;
                request.Timeout = (int)requestTimeout;
                Console.WriteLine("[tilli]: Timeout " + request.Timeout);
                //string postData = theEnvelope.ToJSON();
                string postData = JsonConvert.SerializeObject(new CMessageEnvelope("x", "signature"));
                Console.WriteLine("[tilli]: postData\n" + postData + "\n\n");
                byte[] byteArray = Encoding.UTF8.GetBytes(postData);
                request.ContentType = httpContentType;
                request.ContentType = "application/json";
                Console.WriteLine("[tilli]: ContentType " + httpContentType);
                request.ContentLength = byteArray.Length;
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                WebResponse response = request.GetResponse();
                //Console.WriteLine(((HttpWebResponse)response).StatusDescription);
                dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string responseFromServer = reader.ReadToEnd();
                Console.WriteLine(responseFromServer);
                reader.Close();
                dataStream.Close();
                response.Close();
                Console.WriteLine("[tilli]: SendPing end");
                return true;
            
            }
            catch (UriFormatException)
            {
                return false;
            }
            catch(Exception e)
            {
                Console.WriteLine("[tilli]: SendPing Exception");
                Console.WriteLine("[tilli]: SendPing Error: " + e.Message);
                return false;
            }
        }
        public override bool SendLogs(string pCorrespondence, string pSignature)
        {
            try
            {
                CMessageEnvelope theEnvelope = new CMessageEnvelope(pCorrespondence, pSignature);
                lastMessageEnvelope = theEnvelope;
                WebRequest request = WebRequest.Create(string.Format("{0}{1}:{2}/", scheme, serverAddress, port));
                request.Method = httpMethod;
                request.Timeout = (int)requestTimeout;
                string postData = theEnvelope.ToJSON();
                byte[] byteArray = Encoding.UTF8.GetBytes(postData);
                request.ContentType = httpContentType;
                request.ContentLength = byteArray.Length;
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                WebResponse response = request.GetResponse();
                //Console.WriteLine(((HttpWebResponse)response).StatusDescription);
                dataStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(dataStream);
                string responseFromServer = reader.ReadToEnd();
                Console.WriteLine(responseFromServer);
                reader.Close();
                dataStream.Close();
                response.Close();
                return true;
            }
            catch (UriFormatException)
            {
                return false;
            }
        }
        void OnTimeout()
        {
            if (currentRetry >= maxRetryCount)
            {
                currentRetry = 0;
                OnErrorDelivery();
            }
            else
            {
                System.Timers.Timer theRepeatTimer = new System.Timers.Timer();
                theRepeatTimer.Elapsed += ResendHandler;
                theRepeatTimer.AutoReset = false;
                theRepeatTimer.Interval = tryDelay;
                theRepeatTimer.Start();
            }
        }
        void OnConnectFailure()
        {
            if (currentRetry >= maxRetryCount)
            {
                currentRetry = 0;
                OnErrorDelivery();
            }
            else
            {
                System.Timers.Timer theRepeatTimer = new System.Timers.Timer();
                theRepeatTimer.Elapsed += ResendHandler;
                theRepeatTimer.AutoReset = false;
                theRepeatTimer.Interval = tryDelay;
                theRepeatTimer.Start();
            }
        }
        void OnProtocolError()
        {
            OnErrorDelivery();
        }
        void ResendHandler(object source, System.Timers.ElapsedEventArgs e)
        {
            if (currentRetry >= maxRetryCount)
            {
                currentRetry = 0;
                OnErrorDelivery();
            }
            else
            {
                ++currentRetry;
                Send(lastMessageEnvelope.message, lastMessageEnvelope.signature);
            }
        }
        //
        //Event delegates below
        //
        public event SuccessDeliveryHandler SuccessDelivery;
        public event ErrorDeliveryHandler ErrorDelivery;
        //
        //Events invoking below
        //
        override public void OnSuccessDelivery()
        {
            if (SuccessDelivery != null)
                SuccessDelivery();
        }

        override public void OnErrorDelivery()
        {
            if (ErrorDelivery != null)
                ErrorDelivery(lastMessageEnvelope);
        }
    }
}
