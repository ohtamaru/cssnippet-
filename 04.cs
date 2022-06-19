using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalfbakedTools
{
	/// <summary>
	/// UIのasync/await用なのでひとまずthread unsafe
	/// </summary>
	public class UIOperationLock
	{
		public interface ILockKey
		{
		}

		private int mIdSeed = 1;

		private class EntPointInfo :ILockKey
		{
			public readonly int Id;
			public string File {get; set;}
			public int    Line {get; set;}
			public string Func {get; set;}

			public bool Unlocked { get; set; }

			public EntPointInfo(int id)
			{
				Id = id;
			}

			public override string ToString()
			{
				return $"[{Id}]{File}:{Line}:{Func}";;
			}
		}
		private EntPointInfo mEp = null;

		public ILockKey loc(
				[System.Runtime.CompilerServices.CallerFilePath  ] string arg__file__ = "", 
				[System.Runtime.CompilerServices.CallerLineNumber] int    arg__line__ = 0 , 
				[System.Runtime.CompilerServices.CallerMemberName] string arg__func__ = ""
			)
		{
			if(mEp != null){
//Util.DbgTrac.eout($"[{mEp.Id}]{mEp.File}:{mEp.Line}:{mEp.Func}");
				throw new InvalidOperationException();
			}
			var key = new EntPointInfo(mIdSeed++){
				File = arg__file__, 
				Line = arg__line__, 
				Func = arg__func__, 
			};
			mEp = key;

			return key;
		}

#if false
		private int mDispatcherQueueSampleCount = 0;

		public UIOperationLock()
		{
			System.Windows.Application.Current.Dispatcher.Hooks.OperationPosted += (sender, e) => {
				System.Threading.Interlocked.Increment(ref mDispatcherQueueSampleCount);
			};
			System.Windows.Application.Current.Dispatcher.Hooks.OperationStarted += (sender, e) => {
				System.Threading.Interlocked.Decrement(ref mDispatcherQueueSampleCount);
			};
			System.Windows.Application.Current.Dispatcher.Hooks.OperationAborted += (sender, e) => {
				System.Threading.Interlocked.Decrement(ref mDispatcherQueueSampleCount);
			};
			System.Windows.Application.Current.Dispatcher.Hooks.DispatcherInactive += (sender, e) => {
				System.Threading.Interlocked.And(ref mDispatcherQueueSampleCount, 0);
			};
		}
#endif

		public void unl(
				ILockKey key, 
				[System.Runtime.CompilerServices.CallerFilePath  ] string arg__file__ = "", 
				[System.Runtime.CompilerServices.CallerLineNumber] int    arg__line__ = 0 , 
				[System.Runtime.CompilerServices.CallerMemberName] string arg__func__ = ""
			)
		{
			var keyimpl = key as EntPointInfo;
			if(keyimpl == null){
//Util.DbgTrac.eout("");
				throw new ArgumentException();
			}
			if(keyimpl.Unlocked){
//Util.DbgTrac.eout($"[{keyimpl.Id}]{keyimpl.File}:{keyimpl.Line}:{keyimpl.Func}");
				throw new ArgumentException();
			}
			if(mEp != keyimpl){
//Util.DbgTrac.eout("");
				throw new ArgumentException();
			}
			mEp = null;
			keyimpl.Unlocked = true;
		}
		
		private int mWaitingId;
		private bool mClosing;

		public bool wait()
		{
			if(mClosing){
				return false;
			}
			mWaitingId += 1;
			var waitingid = mWaitingId;
//Util.DbgTrac.dout($"{waitingid},{mWaitingId}");

			while(mEp != null){
//Util.DbgTrac.dout($"{waitingid},{mWaitingId}");
				//await Livet.DispatcherHelper.UIDispatcher.BeginInvoke(() => {});
				//await System.Threading.Tasks.Task.Delay(100);
				System.Windows.Application.Current.Dispatcher.Invoke(() => {}, System.Windows.Threading.DispatcherPriority.Background, new object[]{});
				if(mClosing){
					return false;
				}
				if(waitingid != mWaitingId){
//Util.DbgTrac.dout($"{waitingid},{mWaitingId}");
					return false;
				}
			}
//Util.DbgTrac.dout($"{waitingid},{mWaitingId}");
			return true;
		}

		public void joinForClose()
		{
			if(mClosing){
				throw new InvalidOperationException();
			}
			mClosing = true;

			int retrycount = 0;

			while(mEp != null){
				if(retrycount >= 10){
					Util.DbgTrac.eout($"[{mEp.Id}]{mEp.File}:{mEp.Line}:{mEp.Func}");
					throw new Exception($"[{mEp.Id}]{mEp.File}:{mEp.Line}:{mEp.Func}");
				}
				retrycount += 1;

				//await Livet.DispatcherHelper.UIDispatcher.BeginInvoke(() => {});
				//await System.Threading.Tasks.Task.Delay(100);
				System.Windows.Application.Current.Dispatcher.Invoke(() => {}, System.Windows.Threading.DispatcherPriority.Background, new object[]{});
			}
		}
	}
}
