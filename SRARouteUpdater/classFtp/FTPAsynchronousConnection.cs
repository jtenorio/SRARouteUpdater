using System;
using System.Collections;
using System.Threading;
using System.IO;

namespace FTPClient
{
	/// <summary>
	/// Summary description for FTPAsynchronousConnection.
	/// </summary>
	public class FTPAsynchronousConnection : FTPConnection
	{
		private struct FileTransferStruct
		{
			public string RemoteFileName;
			public string LocalFileName;
			public FTPFileTransferType Type;
		}

		private ArrayList threadPool;
		private Queue sendFileTransfersQueue;
		private Queue getFileTransfersQueue;
		private Queue deleteFileQueue;
		private Queue setCurrentDirectoryQueue;
		private Queue makeDirQueue;
		private Queue removeDirQueue;
		private System.Timers.Timer timer;
				
		public FTPAsynchronousConnection() : base()
		{
			this.threadPool = new ArrayList();
			this.sendFileTransfersQueue = new Queue();
			this.getFileTransfersQueue = new Queue();
			this.deleteFileQueue = new Queue();
			this.setCurrentDirectoryQueue = new Queue();
			this.makeDirQueue = new Queue();
			this.removeDirQueue = new Queue();
			this.timer = new System.Timers.Timer(100);
			this.timer.Elapsed+=new System.Timers.ElapsedEventHandler(ManageThreads);
			this.timer.Start();
		}

		public override void Open(string remoteHost, string user, string password)
		{
			base.Open(remoteHost, user, password);
		}

		public override void Open(string remoteHost, string user, string password, FTPMode mode)
		{
			base.Open(remoteHost, user, password, mode);
		}

		public override void Open(string remoteHost, int remotePort, string user, string password)
		{
			base.Open(remoteHost, remotePort, user, password);
		}
		
		public override void Open(string remoteHost, int remotePort, string user, string password, FTPMode mode)
		{
			base.Open(remoteHost, remotePort, user, password, mode);
		}
		
		private Thread CreateGetFileThread(string remoteFileName, string localFileName, FTPFileTransferType type)
		{
			FileTransferStruct ft = new FileTransferStruct();
			ft.LocalFileName = localFileName;
			ft.RemoteFileName = remoteFileName;
			ft.Type = type;
			this.getFileTransfersQueue.Enqueue(ft);

			Thread thread = new Thread(new ThreadStart(GetFileFromQueue));
			thread.Name = "GetFileFromQueue " + remoteFileName + ", " + localFileName + ", " + type.ToString();;
			return thread;
		}

		public override void GetFile(string remoteFileName, FTPFileTransferType type)
		{
			GetFile(remoteFileName, Path.GetFileName(remoteFileName), type);
		}

		public override void GetFile(string remoteFileName, string localFileName, FTPFileTransferType type)
		{
			EnqueueThread(CreateGetFileThread(remoteFileName, localFileName, type));
		}

		private void GetFileFromQueue()
		{
			FileTransferStruct ft = (FileTransferStruct)this.getFileTransfersQueue.Dequeue();
			base.GetFile(ft.RemoteFileName, ft.LocalFileName, ft.Type);
		}

		private Thread CreateSendFileThread(string localFileName, string remoteFileName, FTPFileTransferType type)
		{
			FileTransferStruct ft = new FileTransferStruct();
			ft.LocalFileName = localFileName;
			ft.RemoteFileName = remoteFileName;
			ft.Type = type;
			this.sendFileTransfersQueue.Enqueue(ft);

			Thread thread = new Thread(new ThreadStart(SendFileFromQueue));
			thread.Name = "GetFileFromQueue " + localFileName + ", " + remoteFileName + ", " + type.ToString();;
			return thread;
		}

		public override void SendFile(string localFileName, FTPFileTransferType type)
		{
			SendFile(localFileName, Path.GetFileName(localFileName), type);
		}
		
		public override void SendFile(string localFileName, string remoteFileName, FTPFileTransferType type)
		{
			EnqueueThread(CreateSendFileThread(localFileName, remoteFileName, type));
		}

		private void SendFileFromQueue()
		{
			FileTransferStruct ft = (FileTransferStruct)this.sendFileTransfersQueue.Dequeue();
			base.SendFile(ft.LocalFileName, ft.RemoteFileName, ft.Type);
		}

		public override void DeleteFile(String remoteFileName)
		{
			EnqueueThread(CreateDeleteFileThread(remoteFileName));
		}

		private Thread CreateDeleteFileThread(String remoteFileName)
		{
			this.deleteFileQueue.Enqueue(remoteFileName);

			Thread thread = new Thread(new ThreadStart(DeleteFileFromQueue));
			thread.Name = "DeleteFileFromQueue " + remoteFileName;
			return thread;
		}
		
		private void DeleteFileFromQueue()
		{
			base.DeleteFile((string)this.deleteFileQueue.Dequeue());
		}

		public override void SetCurrentDirectory(String remotePath)
		{
			EnqueueThread(CreateSetCurrentDirectoryThread(remotePath));
		}

		private Thread CreateSetCurrentDirectoryThread(String remotePath)
		{
			this.setCurrentDirectoryQueue.Enqueue(remotePath);

			Thread thread = new Thread(new ThreadStart(SetCurrentDirectoryFromQueue));
			thread.Name = "SetCurrentDirectoryFromQueue " + remotePath;
			return thread;
		}

		private void SetCurrentDirectoryFromQueue()
		{
			base.SetCurrentDirectory((string)this.setCurrentDirectoryQueue.Dequeue());
		}

		public override void MakeDir(string directoryName)
		{
			EnqueueThread(CreateMakeDirFromQueueThread(directoryName));
		}

		private Thread CreateMakeDirFromQueueThread(string directoryName)
		{
			this.makeDirQueue.Enqueue(directoryName);

			Thread thread = new Thread(new ThreadStart(MakeDirFromQueue));
			thread.Name = "MakeDirFromQueue " + directoryName;
			return thread;
		}

		private void MakeDirFromQueue()
		{
			base.MakeDir((String) this.makeDirQueue.Dequeue());
		}
			
		public override void RemoveDir(string directoryName)
		{
			EnqueueThread(CreateRemoveDirFromQueue(directoryName));
		}

		private Thread CreateRemoveDirFromQueue(string directoryName)
		{
			this.removeDirQueue.Enqueue(directoryName);

			Thread thread = new Thread(new ThreadStart(RemoveDirFromQueue));
			thread.Name = "RemoveDirFromQueue " + directoryName;
			return thread;
		}

		private void RemoveDirFromQueue()
		{
			base.RemoveDir((String) this.removeDirQueue.Dequeue());
		}

		public override void Close()
		{
			WaitAllThreads();
			base.Close();
		}

		private void ManageThreads(Object state, System.Timers.ElapsedEventArgs e)
		{
			Thread thread;
			try
			{
				LockThreadPool();
				thread = PeekThread();
				if(thread != null)
				{
					switch (thread.ThreadState)
					{
						case ThreadState.Unstarted:
							LockThreadPool();
							thread.Start();
							UnlockThreadPool();
							break;
						case ThreadState.Stopped:
							LockThreadPool();
							DequeueThread();
							UnlockThreadPool();
							break;
					}
				}
				UnlockThreadPool();
			}
			catch
			{
				UnlockThreadPool();
			}
		}
		
		private void WaitAllThreads()
		{
			while(this.threadPool.Count!=0)
			{
				Thread.Sleep(100);
			}
		}

		private void EnqueueThread(Thread thread)
		{
			LockThreadPool();
			this.threadPool.Add(thread);
			UnlockThreadPool();
		}
		private Thread DequeueThread()
		{
			Thread thread;
			LockThreadPool();
			thread = (Thread)this.threadPool[0];
			this.threadPool.RemoveAt(0);
			UnlockThreadPool();
			return thread;
		}

		private Thread PeekThread()
		{
			Thread thread = null;
			LockThreadPool();
			if(this.threadPool.Count > 0)
			{
				thread = (Thread)this.threadPool[0];
			}
			UnlockThreadPool();
			return thread;
		}

		private void LockThreadPool()
		{
			Monitor.Enter(this.threadPool);
		}

		private void UnlockThreadPool()
		{
			Monitor.Exit(this.threadPool);
		}
	}
}
