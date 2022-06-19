
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Configuration;

namespace Util
{
	static class DbgTrac
	{
		[System.Runtime.InteropServices.DllImport("kernel32.dll")]
		private static extern uint GetCurrentThreadId();

		private static int sPId;
		private static System.Threading.Mutex sMutex;
		private static string sFilePath;
		
		private const string CONFIG_KEY_LEVEL           = "Util.DbgTrac:Level";
		private const string CONFIG_KEY_THROW_EXCEPTION = "Util.DbgTrac:ThrowException";
		private static int  sConfigLevel = 0;
		private static bool sConfigThrowException = false;

		private static string sSlfAsmFileName;

		static DbgTrac()
		{
			if(ConfigurationManager.AppSettings.AllKeys.Select((string _a) => _a.ToUpperInvariant()).Contains(CONFIG_KEY_LEVEL.ToUpperInvariant())
					&& (!int.TryParse(ConfigurationManager.AppSettings[CONFIG_KEY_LEVEL], out sConfigLevel)
						|| sConfigLevel <= 0)){
				sConfigLevel = 0;
				// int.TryParse set 0 when error
				return;
			}
			if(ConfigurationManager.AppSettings.AllKeys.Select((string _a) => _a.ToUpperInvariant()).Contains(CONFIG_KEY_THROW_EXCEPTION.ToUpperInvariant())
					&& !bool.TryParse(ConfigurationManager.AppSettings[CONFIG_KEY_THROW_EXCEPTION], out sConfigThrowException)){
				// bool.TryParse set false when error
				sConfigThrowException = false;
			}

			if(sConfigLevel == 0){
				return;
			}
			sPId = System.Diagnostics.Process.GetCurrentProcess().Id;
			sSlfAsmFileName = System.IO.Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location);

			string appabspath = System.Reflection.Assembly.GetEntryAssembly().Location;
			string appfolder  = System.IO.Path.GetDirectoryName(appabspath);
			if(!System.IO.Directory.Exists(appfolder)){
				sConfigLevel = 0;
				// 権限の関係で'おそらく'自由にフォルダの作成ができない
				// フォルダに事前に実行ユーザーにアクセス権限を与えておく
				return;
			}
			string logabspath = System.IO.Path.Combine(
				appfolder, 
				string.Format(
					"{0}_{1}.log", 
					System.IO.Path.GetFileNameWithoutExtension(appabspath), 
					System.Diagnostics.Process.GetCurrentProcess().StartTime.ToString("yyyyMMdd_HHmmss_fff")
				)
			);
			var mutex = new System.Threading.Mutex(false, String.Format("Local\\{0}#Util.Trac", logabspath.Replace(System.IO.Path.DirectorySeparatorChar, '/')));

			sFilePath = logabspath;
			sMutex = mutex;
		}

		private static string f_callerinfo()
		{
			string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");

			StackFrame CallStack = new StackFrame(2, true);
			string clss = CallStack.GetMethod().ReflectedType.FullName;
			string file = CallStack.GetFileName();
			int    line = CallStack.GetFileLineNumber();
			string func = CallStack.GetMethod().Name;

			file = file.Substring(file.LastIndexOf('\\') + 1);

			int  mtid = System.Threading.Thread.CurrentThread.ManagedThreadId;
			uint ntid = GetCurrentThreadId();

			return now + " " + sPId.ToString("X8") + ":" + ntid.ToString("X8") + "(" + string.Format("{0,4}", mtid) + ") " + sSlfAsmFileName + ":" + file + ":" + line + ":" + clss + ":" + func;
		}

		private static void f_out(
				string arg_cap, 
				string arg_msg
			)
		{
			if(!sMutex.WaitOne(1000)){
				sConfigLevel = 0;
				if(sConfigThrowException){
					throw new TimeoutException();
				}
			}

			try{
				using(System.IO.FileStream fs = System.IO.File.Open(
						DbgTrac.sFilePath, 
						System.IO.FileMode.Append, 
						System.IO.FileAccess.Write, 
						System.IO.FileShare.ReadWrite)){
					using(System.IO.StreamWriter sw = new System.IO.StreamWriter(fs)){
						sw.WriteLine(arg_cap);
						if (!string.IsNullOrEmpty(arg_msg))
						{
							sw.WriteLine("  " + arg_msg.Replace("\n", "\n  "));
						}
						sw.Flush();
						fs.Flush();

						sw.Close();
					}
					fs.Close();
				}
			}catch(Exception){
				sConfigLevel = 0;
				sMutex.ReleaseMutex();
				if(sConfigThrowException){
					throw;
				}
			}
			sMutex.ReleaseMutex();
		}

		public static void dout(
			  string arg_s = null, 
				[System.Runtime.CompilerServices.CallerFilePath  ] string arg__file__ = "", 
				[System.Runtime.CompilerServices.CallerLineNumber] int    arg__line__ = 0 , 
				[System.Runtime.CompilerServices.CallerMemberName] string arg__func__ = ""
			)
		{
			if(DbgTrac.sConfigLevel < 4){
				return;
			}
			string cap = "[d] " + f_callerinfo();
			f_out(cap, arg_s);
		}

		public static void iout(
			  string arg_s = null, 
				[System.Runtime.CompilerServices.CallerFilePath  ] string arg__file__ = "", 
				[System.Runtime.CompilerServices.CallerLineNumber] int    arg__line__ = 0 , 
				[System.Runtime.CompilerServices.CallerMemberName] string arg__func__ = ""
			)
		{
			if(DbgTrac.sConfigLevel < 3){
				return;
			}
			string cap = "[i] " + f_callerinfo();
			f_out(cap, arg_s);
		}

		public static void wout(
			  string arg_s = null, 
				[System.Runtime.CompilerServices.CallerFilePath  ] string arg__file__ = "", 
				[System.Runtime.CompilerServices.CallerLineNumber] int    arg__line__ = 0 , 
				[System.Runtime.CompilerServices.CallerMemberName] string arg__func__ = ""
			)
		{
			if(DbgTrac.sConfigLevel < 2){
				return;
			}
			string cap = "[w] " + f_callerinfo();
			f_out(cap, arg_s);
		}

		public static void eout(
			  string arg_s = null, 
				[System.Runtime.CompilerServices.CallerFilePath  ] string arg__file__ = "", 
				[System.Runtime.CompilerServices.CallerLineNumber] int    arg__line__ = 0 , 
				[System.Runtime.CompilerServices.CallerMemberName] string arg__func__ = ""
			)
		{
			if(DbgTrac.sConfigLevel < 1){
				return;
			}
			string cap = "[e] " + f_callerinfo();
			f_out(cap, arg_s);
		}

	}
}
