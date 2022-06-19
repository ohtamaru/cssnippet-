#define REFERENCED_SYSTEM_WINDOW_FORMS

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Util
{
	static class DbgMisc
	{
		public static string[] callstk()
		{
			try{
				Stack<string> st = new Stack<string>();
				
				foreach(var e in new System.Diagnostics.StackTrace(1, true).GetFrames()){
					int    line = -1;
					string file = "<unk>";
					string clss = "<unk>";
					string func = "<unk>";
					
					try{
						line = e.GetFileLineNumber();
					} catch {
					}
					try{
						file = e.GetFileName();
						file = file.Substring(file.LastIndexOf('\\') + 1);
					} catch {
					}
					try{
						clss = e.GetMethod().ReflectedType.FullName;
					} catch {
					}
					try{
						func = e.GetMethod().Name;
					} catch {
					}
					
					st.Push(string.Format("{0}({1})...{2}:{3}", file, line, clss, func));
				}

				return st.ToArray();

			} catch {
				return new string[] { "<unk>(-1) ... <unk>:<unk>", };
			}
		}

		private const System.Reflection.BindingFlags OBJDUMP_TARGET_FIELDS     = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public;
		private const System.Reflection.BindingFlags OBJDUMP_TARGET_PROPERTIES = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public;

		private static string f_objdump(
				object arg_obj, 
				List<object> arg_foregoing
			)
		{
			if(arg_obj == null){
				return "null";
			}
			Type type = arg_obj.GetType();

			if(type.IsEnum) {
				return string.Format("(E:{0}){1}", type.Name, arg_obj.ToString());
			} else if(arg_obj is char) {
				return string.Format("'{0}'", (char)arg_obj);
			} else if(arg_obj is float) {
				var ba = BitConverter.GetBytes((float)arg_obj);
				return string.Format("({0}){1}{{{2}}}", type.Name, arg_obj.ToString(), BitConverter.ToString(ba));
			} else if(arg_obj is double) {
				var ba = BitConverter.GetBytes((double)arg_obj);
				return string.Format("({0}){1}{{{2}}}", type.Name, arg_obj.ToString(), BitConverter.ToString(ba));
			} else if(type.IsPrimitive
					|| arg_obj is Decimal){
				return string.Format("({0}){1}", type.Name, arg_obj.ToString());
			}else if(arg_obj is string){
				return string.Format("\"{0}\"", (string)arg_obj);
			}

			int pseudoid = arg_foregoing.Count();
			arg_foregoing.Add(arg_obj);

			// 
			if((type.IsGenericType
						&& type.GetGenericTypeDefinition() == typeof(System.Collections.Generic.IEnumerable<>))
					|| arg_obj is System.Collections.IEnumerable){

				var elmstrs = new List<string>();
				foreach(var e_ in (dynamic)arg_obj){
					if(arg_foregoing.Contains(e_)){
						elmstrs.Add(string.Format("({0})<{1}...>", type.Name, arg_foregoing.IndexOf(e_)));
					}else{
						elmstrs.Add(f_objdump(e_, arg_foregoing));
					}
				}
				return string.Format("({0})<{1}>:[{2}]", type.Name, pseudoid, string.Join(",", elmstrs));
			}
			
			// class, struct
			var memstrs = new List<string>();

			foreach(var field_ in type.GetFields(OBJDUMP_TARGET_FIELDS)){
				object fieldval = field_.GetValue(arg_obj);

				if(arg_foregoing.Contains(fieldval)){
					memstrs.Add(string.Format("{0}=<{1}...>", field_.Name, arg_foregoing.IndexOf(fieldval)));
				}else{
					memstrs.Add(string.Format("{0}={1}", field_.Name, f_objdump(fieldval, arg_foregoing)));
				}
			}

			foreach(var propa_ in type.GetProperties(OBJDUMP_TARGET_PROPERTIES)){
				if(!propa_.CanRead){
					continue;
				}
				try{
					object propaval = propa_.GetValue(arg_obj, null);

					if(arg_foregoing.Contains(propaval)){
						memstrs.Add(string.Format("{0}=<{1}...>", propa_.Name, arg_foregoing.IndexOf(propaval)));
					}else{
						memstrs.Add(string.Format("{0}={1}", propa_.Name, f_objdump(propaval, arg_foregoing)));
					}
				}catch{
					memstrs.Add(string.Format("{0}=??", propa_.Name));
				}
			}
			return string.Format("({0})<{1}>:{{{2}}}", type.Name, pseudoid, string.Join(",", memstrs.ToArray()));
		}

		public static string objdump(object arg_obj)
		{
			return f_objdump(arg_obj, new List<object>());
		}

	}
}
