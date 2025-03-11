using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace YHIUYIUL
{
    public class SocketOperator
    {
        private Operator opCode;
        private string[] param;
        private const string Runner = "Runner";
        private const string InputRunner = "InputRunner";
        public enum Operator
        {
            None,
            CreatRunner,
            InputRunner,
        }
        public SocketOperator(Operator op, params string[] args)
        {
            opCode = op;
            param = args;
        }
        public string this [int i]
        {
            get
            {
                return param[i];
            }
            private set
            {
                param[i] = value;
            }     
        }
        public Operator OpCode => opCode;
        public int ParamCount => param.Length;

        public override string ToString()
        {
            return $"{OperatorToString()}:{string.Join(':', param)}";
        }
        public static SocketOperator Parse(string str)
        {
            if (string.IsNullOrEmpty(str)) return new SocketOperator(Operator.None);
            var split = str.Split(':').ToList();
            Operator opCode = StringToOperator(split[0]);
            string[] param = new string[0];
            if (split.Count > 1)
            {
                param = new string[split.Count - 1];
                split.CopyTo(1, param, 0, split.Count - 1);
            }
            return new SocketOperator(opCode, param);
        }

        public string OperatorToString()
        {
            return OperatorToString(opCode);
        }
        public static string OperatorToString(Operator opCode) =>
        opCode switch
        {
            Operator.CreatRunner => Runner,
            Operator.InputRunner => InputRunner,
            _ => "_:Unnamed Op"
        };
        public static Operator StringToOperator(string str) =>
        str switch
        {
            Runner => Operator.CreatRunner,
            InputRunner => Operator.InputRunner,
            _ => Operator.None,
        };
    }
    
    public class ModalOperater
    {

    }
}
