using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MVVM_Bonus.Services
{
    public class Mediator
    {
        static IDictionary<string, List<Action<object>>> pl_Dict = new Dictionary<string, List<Action<object>>>();

        public static void Subscribe(string token, Action<object> callback)
        {
            if (!pl_Dict.ContainsKey(token))
            {
                List<Action<object>> list = new List<Action<object>>();
                list.Add(callback);
                pl_Dict.Add(token, list);
            }
            else
            {
                bool found = false;
                foreach (var item in pl_Dict[token])
                    if (item.Method.ToString() == callback.Method.ToString())
                        found = true;
                if (!found)
                    pl_Dict[token].Add(callback);
            }
        }
        public static void UnSubscribe(string token, Action<object> callback)
        {
            if (pl_Dict.ContainsKey(token))
                pl_Dict[token].Remove(callback);
        }
        public static void Notify(string token, string args = null)
        {
            if (pl_Dict.ContainsKey(token))
            {
                foreach (var callback in pl_Dict[token])
                    callback(args);

            }
        }
    }
}
