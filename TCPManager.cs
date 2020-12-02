using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TCPManager : MonoBehaviour
{
    public interface ITCPInterface
    {
        void ProcessData(string data);
    }

    public static Dictionary<string, List<ITCPInterface>> MessageList = new Dictionary<string, List<ITCPInterface>>();

    public static TCPManager Instance;

    public enum TCPSide
    {
        SERVER,
        CLIENT,
		HOLOLENS,
        ALL
    }

    public enum ConnectionState
    {
        NotConnected,
        AttemptingConnect,
        Connected
    }

    public TCPSide tcpSide;

	public string strServerIP = "127.0.0.1";

    TCPServer tcpServer;
    TCPClient tcpClient;
	TCPHololensClient tcpHololensClient;

    private void Awake()
    {
        Instance = this;
        switch (tcpSide)
        {
            case TCPSide.ALL:
                InitTCPServer();
                InitTCPClient();
                break;
            case TCPSide.SERVER:
                InitTCPServer();
                break;
            case TCPSide.CLIENT:
                InitTCPClient();
                break;
			case TCPSide.HOLOLENS:
				InitTCPHololensClient();
				break;
        }        
    }

    private void InitTCPServer()
    {
        tcpServer = GetComponent<TCPServer>();
        if (tcpServer == null)
        {
            tcpServer = gameObject.AddComponent<TCPServer>();
        }
    }

    private void InitTCPClient()
    {
        tcpClient = GetComponent<TCPClient>();
        if (tcpClient == null)
        {
            tcpClient = gameObject.AddComponent<TCPClient>();
        }

        tcpClient.connectState = ConnectionState.NotConnected;
    }

	private void InitTCPHololensClient()
	{
		tcpHololensClient = GetComponent<TCPHololensClient>();
		if (tcpHololensClient == null)
		{
			tcpHololensClient = gameObject.AddComponent<TCPHololensClient>();
		}
	}

    private void Start()
    {
        switch (tcpSide)
        {
            case TCPSide.ALL:
                tcpClient.connectState = ConnectionState.NotConnected;
                if (tcpClient.connectState == ConnectionState.NotConnected)
                {
                    tcpServer.StartServer();
                    System.Threading.Thread.Sleep(10);
                    tcpClient.StartConnect(strServerIP);
                }
                break;
            case TCPSide.SERVER:
                tcpServer.StartServer();
                break;
            case TCPSide.CLIENT:
                tcpClient.connectState = ConnectionState.NotConnected;
                if (tcpClient.connectState == ConnectionState.NotConnected)
                {
                    tcpClient.StartConnect(strServerIP);
                }
                break;
			case TCPSide.HOLOLENS:
				tcpHololensClient.Connect(strServerIP, "10000");
				break;
        }
    }

    public void Send(string msg)
    {
        switch (tcpSide)
        {
            case TCPSide.ALL:
            case TCPSide.SERVER:
                tcpServer.Send(msg);
                break;
            case TCPSide.CLIENT:
                tcpClient.Send(msg);
                break;
			case TCPSide.HOLOLENS:
				tcpHololensClient.Send(msg);
				break;
        }
    }

	public void Send(string nm, string msg)
	{
		switch (tcpSide)
		{
			case TCPSide.ALL:
			case TCPSide.SERVER:
				tcpServer.Send(nm, msg);
				break;
		}
	}

	public List<TCPServer.ClientState> GetConnectedClients()
	{
		return tcpServer.GetClients();
	}

    public static void AddMessage(string message, ITCPInterface tcpInterface)
    {
        if (MessageList.ContainsKey(message))
        {
            if (!MessageList[message].Contains(tcpInterface))
            {
                MessageList[message].Add(tcpInterface);
            }
        }
        else
        {
            List<ITCPInterface> newTCPInterface = new List<ITCPInterface>();
            newTCPInterface.Add(tcpInterface);
            MessageList.Add(message, newTCPInterface);
        }
    }

    public static void RemoveMessage(string message, ITCPInterface tcpInterface)
    {
        if (MessageList.ContainsKey(message))
        {
            if (MessageList[message].Contains(tcpInterface))
            {
                MessageList[message].Remove(tcpInterface);

                if (MessageList[message].Count == 0)
                {
                    MessageList.Remove(message);
                }
            }
        }
    }

    public static List<ITCPInterface> GetMessages(string message)
    {
        if (MessageList.ContainsKey(message))
        {
            return MessageList[message];
        }
        return null;
    }
}
