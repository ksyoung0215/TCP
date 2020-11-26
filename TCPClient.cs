using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System;

public class TCPClient : MonoBehaviour
{
	public TCPManager.ConnectionState connectState;
	Socket m_clientSocket;
	byte[] m_readBuffer;

	string strRecvData;
	string strServerIP = "";
	bool bReceived;

	void Awake()
	{
		connectState = TCPManager.ConnectionState.NotConnected;
		m_readBuffer = new byte[1024];
	}
	public void StartConnect(string strIP)
	{
		strServerIP = strIP;
		StartCoroutine(ConnectRoutine());
	}

	IEnumerator ConnectRoutine()
	{
		m_clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		System.IAsyncResult result = null;
		try
		{
			result = m_clientSocket.BeginConnect(strServerIP, 10000, EndConnect, null);
			bool connectSuccess = result.AsyncWaitHandle.WaitOne(System.TimeSpan.FromSeconds(10));
			if (!connectSuccess)
			{
				m_clientSocket.Close();
				Debug.LogError(string.Format("Client unable to connect. Failed"));
			}
		}
		catch (System.Exception ex)
		{
			Debug.LogError(string.Format("Client exception on beginconnect: {0}", ex.Message));
		}
		connectState = TCPManager.ConnectionState.AttemptingConnect;

		while (connectState != TCPManager.ConnectionState.Connected)
		{
			yield return null;
		}

		if (result != null)
		{
			while (connectState == TCPManager.ConnectionState.Connected)
			{
				bReceived = false;
				IAsyncResult asyncResult = m_clientSocket.BeginReceive(m_readBuffer, 0, m_readBuffer.Length, SocketFlags.None, EndReceiveData, null);

				while (!asyncResult.IsCompleted)
				{
					yield return null;
				}

				while (!bReceived)
				{
					yield return null;
				}

				Debug.Log("Client Recv : " + strRecvData);

				if (strRecvData.Equals("Disconnect"))
				{
					connectState = TCPManager.ConnectionState.NotConnected;
				}

				List<TCPManager.ITCPInterface> tcpList = TCPManager.GetMessages("TCP");
				foreach (TCPManager.ITCPInterface t in tcpList)
				{
					t.ProcessData(strRecvData);
				}
			}
		}
	}

	void EndConnect(System.IAsyncResult iar)
	{
		m_clientSocket.EndConnect(iar);
		m_clientSocket.NoDelay = true;
		connectState = TCPManager.ConnectionState.Connected;
		Debug.Log("Client connected");
	}
	void OnDestroy()
	{
		if (m_clientSocket != null)
		{
			Send("Disconnect");
		}
	}

	void EndReceiveData(System.IAsyncResult iar)
	{
		if (iar.IsCompleted)
		{
			try
			{
				int numBytesReceived = m_clientSocket.EndReceive(iar);
				ProcessData(numBytesReceived);
			}
			catch (Exception e)
			{
				Debug.Log(e.Message);
			}
		}
	}
	void ProcessData(int numBytesRecv)
	{
		strRecvData = Encoding.Default.GetString(m_readBuffer, 0, numBytesRecv);
		bReceived = true;
	}

	void SendReply(byte[] msgArray, int len)
	{
		string temp = Encoding.Default.GetString(msgArray, 0, len);
		Debug.Log(string.Format("Client sending: len: {1} '{0}'", temp, len));
		m_clientSocket.BeginSend(msgArray, 0, len, SocketFlags.None, EndSend, msgArray);
	}
	void EndSend(System.IAsyncResult iar)
	{
		m_clientSocket.EndSend(iar);
		byte[] msg = (iar.AsyncState as byte[]);
		string temp = Encoding.Default.GetString(msg, 0, msg.Length);
		Debug.Log(string.Format("Client sent: '{0}'", temp));
		System.Array.Clear(msg, 0, msg.Length);
		msg = null;

		if (temp.Equals("Disconnect"))
		{
			m_clientSocket.Close();
			m_clientSocket = null;
		}
	}

	public void Send(string msg)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(msg);
		SendReply(bytes, bytes.Length);
	}
}
