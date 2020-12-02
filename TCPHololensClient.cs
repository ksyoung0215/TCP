using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using UnityEngine;
using System.Text;

#if !UNITY_EDITOR
using System.Threading.Tasks;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.Networking;
#endif

public class TCPHololensClient : MonoBehaviour
{
	public static TCPHololensClient Instance;
#if !UNITY_EDITOR
	StreamSocketListener listener;
	StreamSocket socket;
	
	private StreamWriter writer;
	private StreamReader reader;

	Task task;	
	Task sendTask;
#endif

	string lastPacket = null;	

	private void Awake()
	{
		Instance = this;
	}

	public void Connect(string host, string port)
	{
		ConnectUWP(host, port);
	}

	private async void ConnectUWP(string host, string port)
	{
#if !UNITY_EDITOR
		try
		{			
			socket = new StreamSocket();
			HostName serverHost = new HostName(host);
			await socket.ConnectAsync(serverHost, port);
			
			Stream streamOut = socket.OutputStream.AsStreamForWrite();
			writer = new StreamWriter(streamOut){ AutoFlush = true };

			Stream streamIn = socket.InputStream.AsStreamForRead();
			reader = new StreamReader(streamIn);
			
			StartTask();
		}
		catch(Exception e)
		{
			RequestReceiver.Instance.text.text = e.Message;
		}
#endif
	}

	private void StartTask()
	{
#if !UNITY_EDITOR
		task = Task.Run(() => ReadData());
#endif
	}

	public void ReadData()
	{
#if !UNITY_EDITOR
		while (true)
		{
			char[] buffer = new char[256];
			int bytes = reader.Read(buffer, 0, buffer.Length);			

			if(bytes > 0)
			{
				string received = null;
				received = new string(buffer, 0, bytes);
				lastPacket = received;
			}						
		}
#endif
	}

	private void Update()
	{
		if (lastPacket != null)
		{
			List<TCPManager.ITCPInterface> tcpList = TCPManager.GetMessages("TCP");
			foreach (TCPManager.ITCPInterface t in tcpList)
			{
				t.ProcessData(lastPacket);
			}
			lastPacket = null;
		}
	}

	public void Send(string msg)
	{
#if !UNITY_EDITOR
		writer.Write(msg);
#endif
	}

	public bool IsConnected()
	{
#if !UNITY_EDITOR
		return task != null;
#endif
		return false;
	}

	private void OnDestroy()
	{
		StopTCP();
	}

	public void StopTCP()
	{
#if !UNITY_EDITOR
		if (task != null)
		{
			task.Wait();
			socket.Dispose();
			writer.Dispose();
			reader.Dispose();

			socket = null;
			task = null;
		}

		writer = null;
		reader = null;
#endif
	}
}
