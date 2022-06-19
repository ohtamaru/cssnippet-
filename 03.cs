using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HalfbakedTools
{
	public static class Promise
	{
		/// <summary>
		/// resolve only
		/// notes: 動作未検証
		/// </summary>
		/// <param name="action"></param>
		/// <returns></returns>
		public static Task<T> promise<T>(Action<Action<T>> action)
		{
			var tcs = new TaskCompletionSource<T>();
			try{
				action(tcs.SetResult);
			}catch(Exception exp_){
				Util.DbgTrac.eout(exp_.ToString());
				tcs.SetException(exp_);
			}
			return tcs.Task;
		}

		public static Task promise(Action<Action> action)
		{
			var tcs = new TaskCompletionSource();
			try{
				action(tcs.SetResult);
			}catch(Exception exp_){
				Util.DbgTrac.eout(exp_.ToString());
				tcs.SetException(exp_);
			}
			return tcs.Task;
		}

	}
}
