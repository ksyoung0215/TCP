using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System;

public class TCPServer : MonoBehaviour
{
	public enum TestMessageOrder
	{
		NotConnected,
		Connected,
		SendFirstMessage,
		ReceiveFirstMessageReply,
		SendSecondMessage,
		ReceiveSecondMessageReply,
		SendThirdMessage,
		ReceiveThirdMessageReply,
		Error,
		Done
	}
	protected TcpListener m_tcpListener;

	public class ClientState
	{
		public TcpListener tcpListener;
		public Socket clientSocket;
		public TestMessageOrder state;
		public byte[] readBuffer = new byte[1024];
		public byte[] sendBuffer;
		public string strData = "";
		public string strName = "";
		public int id;
		public bool bReceived = false;
	}
	List<ClientState> list_clients = new List<ClientState>();

	public void StartServer()
	{
		m_tcpListener = new TcpListener(IPAddress.Any, 10000);
		m_tcpListener.Start();
		StartListeningForConnections();
	}
	void StartListeningForConnections()
	{
		StartCoroutine(ListenRoutine());
	}
	IEnumerator ListenRoutine()
	{
		ClientState client = new ClientState();
		client.tcpListener = m_tcpListener;		
		list_clients.Add(client);
		client.id = list_clients.Count;

		IAsyncResult result = m_tcpListener.BeginAcceptSocket(AcceptNewSocket, client);

		Debug.Log("SERVER ACCEPTING NEW CLIENTS : ");

		while (client.state != TestMessageOrder.Connected)
		{
			yield return null;
		}

		if (client.state == TestMessageOrder.Connected)
		{
			StartListeningForConnections();
		}

		while (client.state == TestMessageOrder.Connected)
		{
			client.bReceived = false;
			IAsyncResult asyncResult = client.clientSocket.BeginReceive(client.readBuffer, 0, client.readBuffer.Length, SocketFlags.None, EndReceiveData, client);

			while (!asyncResult.IsCompleted)
			{
				yield return null;
			}

			while (!client.bReceived)
			{
				yield return null;
			}

			Debug.Log("Server Recv [" + client.id + "] : " + client.strData);

			if (client.strData.Equals("Disconnect"))
			{
				client.state = TestMessageOrder.NotConnected;
			}

			List<TCPManager.ITCPInterface> tcpList = TCPManager.GetMessages("TCP");
			foreach (TCPManager.ITCPInterface t in tcpList)
			{
				t.ProcessData(client.strData);
			}
		}

		if (client.clientSocket != null)
		{
			client.clientSocket.Close();
			client.clientSocket = null;
		}

		list_clients.Remove(client);
	}

	void AcceptNewSocket(System.IAsyncResult iar)
	{
		ClientState client = (ClientState)iar.AsyncState;

		client.clientSocket = null;
		client.state = TestMessageOrder.NotConnected;
		client.readBuffer = new byte[1024];
		try
		{
			client.clientSocket = m_tcpListener.EndAcceptSocket(iar);
		}
		catch (System.Exception ex)
		{
			//Debug.LogError(string.Format("Exception on new socket: {0}", ex.Message));
		}
		client.clientSocket.NoDelay = true;

		if (client.clientSocket.Connected)
		{
			client.state = TestMessageOrder.Connected;
		}
	}

	//  void AcceptNewSocket(System.IAsyncResult iar)
	//  {
	//      m_clientSocket = null;
	//      m_testClientState = TestMessageOrder.NotConnected;
	//      m_readBuffer = new byte[1024];
	//      try
	//      {
	//          m_clientSocket = m_tcpListener.EndAcceptSocket(iar);
	//      }
	//      catch (System.Exception ex)
	//      {
	//          //Debug.LogError(string.Format("Exception on new socket: {0}", ex.Message));
	//      }
	//      m_clientSocket.NoDelay = true;

	//if (m_clientSocket.Connected)
	//{
	//	m_testClientState = TestMessageOrder.Connected;
	//}        
	//  }
	void SendMessage(ClientState client, byte[] msg)
	{
		if (client.state == TestMessageOrder.Connected)
		{
			client.sendBuffer = msg;
			client.clientSocket.BeginSend(msg, 0, msg.Length, SocketFlags.None, EndSend, client);
		}
	}
	void EndSend(System.IAsyncResult iar)
	{
		ClientState client = (ClientState)iar.AsyncState;
		client.clientSocket.EndSend(iar);
		byte[] msgSent = (client.sendBuffer as byte[]);
		string temp = Encoding.Default.GetString(msgSent);
		Debug.Log(string.Format("Server sent: '{0}'", temp));

		if (temp.Equals("Disconnect"))
		{
			client.clientSocket.Close();
			client.clientSocket = null;
		}
	}

	void EndReceiveData(System.IAsyncResult iar)
	{
		ClientState client = (ClientState)iar.AsyncState;
		int numBytesReceived = client.clientSocket.EndReceive(iar);		
		ProcessData(numBytesReceived, client);
	}
	void ProcessData(int numBytesRecv, ClientState client)
	{
		client.strData = Encoding.Default.GetString(client.readBuffer, 0, numBytesRecv);

		Debug.Log("client.strData : " + client.strData);
		Debug.Log("numBytesRecv : " + numBytesRecv);

		for (int i = 0; i < list_clients.Count; i++)
		{
			Debug.Log(list_clients[i].id + " : " + list_clients[i].strData);
		}

		client.bReceived = true;
	}
	void OnDestroy()
	{
		for (int i = 0; i < list_clients.Count; i++)
		{
			ClientState client = list_clients[i];
			Send(client, "Disconnect");
		}

		if (m_tcpListener != null)
		{
			m_tcpListener.Stop();
			m_tcpListener = null;
		}
	}

	public void Send(string msg)
	{
		for (int i = 0; i < list_clients.Count; i++)
		{
			Send(list_clients[i], msg);
		}
	}

	public void Send(string nm, string msg)
	{
		for (int i = 0; i < list_clients.Count; i++)
		{
			if (list_clients[i].strName.Equals(nm))
			{
				Send(list_clients[i], msg);
				break;
			}
		}
	}

	public void Send(ClientState client, string msg)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(msg);
		SendMessage(client, bytes);
	}

	public List<ClientState> GetClients()
	{
		return list_clients;
	}
}
