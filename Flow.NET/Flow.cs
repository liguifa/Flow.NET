using FlowNET.Common;
using FlowNET.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlowNET
{
    public class Flow
    {
        private WebScocket mWebScocket;

        private Flow()
        {
            this.mWebScocket = new WebScocket();
            this.mWebScocket.Listen(1202);
        }

        public static Flow Create()
        {
            return new Flow();
        }

        public void Publish<T>(T data)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            string message = typeof(T).IsValueType ? data.ToString() : SerializerHelper.SerializeToJSON<T>(data);
            this.mWebScocket.OnNext(message);
        }
    }
}
