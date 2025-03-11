using System;
using System.Xml.Linq;


public class ExpressionTree
{
    public delegate int ReadValue(int index);
    private class Node
    {
        public int type; // 0: 상수, 1: 변수, 2: 연산
        public int value; // 상수 값, 변수 ID, 연산 종류
        public Node? left;
        public Node? right;
        public ReadValue readValue;

        public Node(int type, int value, ReadValue readValue)
        {
            this.type = type;
            this.value = value;
            this.readValue = readValue;
            left = null;
            right = null;
        }
        public int GetValue()
        {

            if (type == 2)
            {
                var i = value switch
                {
                    0 => left.GetValue() + right.GetValue(),
                    1 => left.GetValue() - right.GetValue(),
                    2 => left.GetValue() * right.GetValue(),
                    3 => left.GetValue() / right.GetValue(),
                    _ => throw new NotSupportedException()
                };
                //Console.WriteLine($"left : {left.GetValue()}, right : {right.GetValue()} ,op : {value}, re : {i}");
                return i;
            }
            else if (type == 0)
            {
                return value;
            }
            else if (type == 1)
            {
                return readValue(value);
            }
            else
            {
                throw new TokenException("이상한 토큰이 껴있는데?");
            }
        }
        public override string ToString()
        {
            if (type == 2)
                return $"{left} {value switch {0=>"+", 1 => "-",2 => "*",3 => "/", _=>throw new TokenException("연산의 종류가 이상함"), }} {right}";
            else if (type == 0)
            {
                return $"{type}:'{value}'";
            }
            else if (type == 1)
            {
                return $"{type}:'{readValue(value)}";
            }
            else
            {
                throw new TokenException("이상한 토큰이 껴있는데?");
            }
        }
    }
    private Node? root;

    private bool lastTokenWasOperator; // 마지막 토큰이 연산자였는지 확인
    public ReadValue readValue;

    public ExpressionTree(ReadValue readValue)
    {
        root = null;
        lastTokenWasOperator = true; // 첫 번째 입력은 연산자가 아닌 값이어야 하므로 초기값을 true로 설정
        this.readValue = readValue;
    }
    public void PrintE()
    {
        if (lastTokenWasOperator) throw new TokenException("마지막 토큰은 연산자일 수 없습니다");
        //Console.WriteLine($"{root} = {root.GetValue()}");
    }
    public void Add(int type, int value)
    {
        //Console.WriteLine("Add");
        if ((lastTokenWasOperator && (type == 0 || type == 1)) || (!lastTokenWasOperator && type == 2))
        {
            Node newNode = new Node(type, value, readValue);

            if (root == null)
            {
                //Console.WriteLine("root == null");
                root = newNode;
            }
            else
            {
                if (type == 2)
                {
                    newNode.left = root;
                    root = newNode;
                }
                else
                {
                    //굳이 안돌아도 돼지만 혹시나;;
                    Node valueNode = root;
                    while (valueNode.right != null)
                    {
                        valueNode = valueNode.right;
                    }
                    valueNode.right = newNode;
                }
            }

            // 마지막 토큰을 갱신
            lastTokenWasOperator = (type == 2);
        }
        else
        {
            Console.WriteLine("잘못된 토큰 순서: 값과 연산자가 번갈아가며 입력되어야 합니다.");
            return;
        }
    }
    public int GetValue()
    {
        if (isRootNull()) return 0;
        if (lastTokenWasOperator) throw new TokenException("마지막 토큰이 연잔자임");
        //PrintE();
        //Console.WriteLine(root.GetValue());
        return root.GetValue();
    }
    public bool isRootNull()
    {
        //return isRootNull == null;
        return root == null;
    }
    public bool isReady()
    {
        return !lastTokenWasOperator;
    }
}

public class TokenException : Exception
{
    public TokenException(string message) : base(message){ }
}