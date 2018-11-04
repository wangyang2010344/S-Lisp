using System;
using System.Collections.Generic;
using System.Text;

namespace s
{
    public class Exp
    {
        public enum ExpType
        {
            Exp_Large,
            Exp_Medium,
            Exp_Small,
            Exp_String,
            Exp_Int,
            Exp_Bool,
            Exp_Id,/*id����kvs-path*/
            Exp_Let,
            Exp_LetId,
            Exp_LetSmall,
            Exp_LetRest
        }
        private bool isBracket;
        public bool IsBracket()
        {
            return isBracket;
        }
        private ExpType type;
        public ExpType Exp_type()
        {
            return type;
        }
        /*���Ų���*/
        private Node<Exp> children;
        private Node<Exp> r_children;
        public Node<Exp> Children() { return children; }
        public Node<Exp> R_children() { return r_children; }
        private Token left;
        private Token right;
        public Token Left() { return left; }
        public Token Right() { return right; }
        private Exp(
            ExpType type, 
            Token left,
            Token right,
            Node<Exp> children, 
            Node<Exp> r_children
        )
        {
            this.isBracket = true;
            this.type = type;
            this.left = left;
            this.right = right;
            this.children = children;
            this.r_children = r_children;
        }

        /*������*/
        private Token token;
        public Token Token() { return token; }
        public String Value() { return token.Value(); }
        private int int_value;
        public int Int_Value(){ return int_value;}
        private bool bool_value;
        public bool Bool_Value() { return bool_value; }
        private Node<String> kvs_paths;
        public Node<String> KVS_paths() { return kvs_paths; }
        public Exp(ExpType type, Token token)
        {
            this.isBracket = false;
            this.type = type;
            this.token = token;
            if (type == ExpType.Exp_Int)
            {
                int_value = int.Parse(token.Value());
            }
            if (type == ExpType.Exp_Bool)
            {
                bool_value=(token.Value() == "true");
            }
            if (type == ExpType.Exp_Id)
            {
                String id = token.Value();
                if (id[0] == '.' || id[id.Length - 1] == '.')
                {
                    //throw new LocationException(loc,"���ǺϷ���id���ͣ�������.��ʼ�����");
                    kvs_paths = null;
                }
                else
                {
                    int i = 0;
                    int last_i = 0;
                    Node<String> r = null;
                    bool has_error = false;
                    while (i < id.Length)
                    {
                        char c = id[i];
                        if (c == '.')
                        {
                            String node = id.Substring(last_i, i - last_i);
                            last_i = i + 1;
                            if (node == "")
                            {
                                has_error = true;
                            }
                            else
                            {
                                r = Node<String>.extend(node, r);
                            }
                        }
                        i++;
                    }
                    /*���һ����Ĭ��ƥ��*/
                    r = Node<String>.extend(id.Substring(last_i), r);
                    if (has_error)
                    {
                        throw this.exception(id + "���ǺϷ���id���ͣ�������������.��");/*id�����м���������..*/
                    }
                    else
                    {
                        kvs_paths = Node<String>.reverse(r);
                    }
                }
            }
        }
        public void toString(StringBuilder sb)
        {
            if (isBracket)
            {
                sb.Append(left.Value());
                for (Node<Exp> t = children; t != null; t = t.Rest())
                {
                    sb.Append(t.First().ToString()).Append(" ");
                }
                sb.Append(right.Value());
            }
            else
            {
                sb.Append(token.ToString());
            }
        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            toString(sb);
            return sb.ToString();
        }
        public LocationException exception(String msg)
        {
            if (isBracket)
            {
                return new LocationException(left.Loc(), msg);
            }
            else
            {
                return new LocationException(token.Loc(), msg);
            }
        }
        public void warn(String msg)
        {
            if (isBracket)
            {
                Console.Write(left.Loc().ToString());
            }
            else
            {
                Console.Write(token.Loc().ToString());
            }
            Console.WriteLine(msg);
        }

        /// <summary>
        /// ����Let�е�ID���ʽ��
        /// </summary>
        /// <param name="k"></param>
        /// <returns></returns>
        static Exp resetLetID(Exp k)
        {
            if (k.Value().IndexOf('.') < 0)
            {
                return new Exp(ExpType.Exp_LetId, k.Token());
            }
            else
            {
                throw k.exception("let���ʽ�У�" + k.ToString() + "���ǺϷ���key-id����");/*let-id�м䲻�������κε�.*/
            }
        }
        /// <summary>
        /// let���ʽ�ģ�����ƥ��Ĳ��֣��������䷴ת����������
        /// </summary>
        /// <param name="k"></param>
        /// <returns></returns>
        static Exp resetLetSmall(Exp small)
        {
            Node<Exp> vs = small.R_children();
            Node<Exp> children=null;
            if (vs != null)
            {
                Exp k = vs.First();
                vs = vs.Rest();
                if (k.Exp_type() == ExpType.Exp_Id)
                {
                    String v = k.Value();
                    if (v.StartsWith("..."))
                    {
                        v = v.Substring(3);
                        if (v.IndexOf('.') < 0)
                        {
                            children = Node<Exp>.extend(
                                new Exp(
                                    ExpType.Exp_LetRest,
                                    new Token(v,k.Token().Old_Value(),k.Token().Token_type(),k.Token().Loc())
                                ),
                                children
                             );
                        }
                        else
                        {
                            throw k.exception("let���ʽ�У�" + k.ToString() + "���ǺϷ���ʣ��ƥ��ID");/*ʣ��ƥ���id��ֻ����������ͷ�ĵ�*/
                        }
                    }
                    else
                    {
                        /*
                         * ���һ��ֻ����ͨ��ID
                         */
                        children = Node<Exp>.extend(resetLetID(k),children);
                    }
                }
            }
            while (vs != null)
            {
                Exp k = vs.First();
                vs = vs.Rest();
                if (k.Exp_type() == ExpType.Exp_Small)
                {
                    children = Node<Exp>.extend(resetLetSmall(k), children);
                }
                else if (k.Exp_type() == ExpType.Exp_Id)
                {
                    children = Node<Exp>.extend(resetLetID(k),children);
                }
                else
                {
                    throw k.exception("Let���ʽ�У����ǺϷ���key����" + k.ToString());/*Let���ʽ�У�key���ֳ����������ͣ������֡������ȣ���Ӧ���������*/
                }
            }
            return new Exp(
                ExpType.Exp_LetSmall,
                small.Left(),
                small.Right(),
                children,
                null
            );
        }

        /// <summary>
        /// let���ʽkvkv�Ĳ��֣������Ƿ�ת�ģ���vkvk...
        /// </summary>
        /// <param name="vks"></param>
        /// <returns></returns>
        static Node<Exp> resetLetVKS(Node<Exp> vks)
        {
            Node<Exp> children = null;
            while (vks != null)
            {
                Exp v = vks.First();
                children = Node<Exp>.extend(v,children);
                vks = vks.Rest();
                if (vks != null)
                {
                    Exp k = vks.First();
                    vks = vks.Rest();

                    if (k.Exp_type() == ExpType.Exp_Id)
                    {
                        children=Node<Exp>.extend(resetLetID(k),children);
                    }
                    else if (k.Exp_type() == ExpType.Exp_Small)
                    {
                        if (k.Children() == null)
                        {
                            k.warn("Let���ʽ��������Ŀ�()������:" + v.ToString());/*let���ʽ�У�������Ŀ�*/
                        }
                        children=Node<Exp>.extend(resetLetSmall(k),children);
                    }
                    else
                    {
                        throw k.exception("let���ʽ�У����Ϸ���key����:" + k.ToString());/*let���ʽ�У����ǺϷ���key���ͣ�����id��()*/
                    }
                }
                else
                {
                    throw v.exception("let���ʽ���ڴ���value:" + v.ToString() + "ƥ�䣬ȴ������let���ʽ");/*let���ʽ����ȫ*/
                }
            }
            return children;
        }
        /// <summary>
        /// ��麯���ڵ����ú���
        /// </summary>
        /// <param name="children"></param>
        static void check_Large(Node<Exp> vs)
        {
            while (vs != null)
            {
                Exp v = vs.First();
                vs = vs.Rest();
                if (vs != null)
                {
                    ExpType t = v.Exp_type();
                    if(!(t==ExpType.Exp_Let || t==ExpType.Exp_Small || t==ExpType.Exp_Medium))
                    {
                        v.warn("�����ж���������ı��ʽ������:"+v.ToString());
                    }
                }
            }
        }
        public static Exp Parse(Node<Token> tokens)
        {
            Location root_loc = new Location(0, 0, 0);
            Token root_left = new Token("{", "{", s.Token.TokenType.Token_BracketLeft, root_loc);
            Token root_right = new Token("}", "}", s.Token.TokenType.Token_BracketRight, root_loc);
            Exp exp = new Exp(ExpType.Exp_Large, root_left, root_right, null, null);
            Node<Exp> caches = Node<Exp>.extend(exp, null);
            Node<Token> xs = tokens;
            Node<Exp> children = null;
            while (xs != null)
            {
                Token x = xs.First();
                xs = xs.Rest();
                if (x.Token_type() == s.Token.TokenType.Token_BracketRight)
                {
                    Exp.ExpType tp;
                    if (x.Value() == ")")
                    {
                        tp = ExpType.Exp_Small;
                    }
                    else if (x.Value() == "]")
                    {
                        tp = ExpType.Exp_Medium;
                    }
                    else
                    {
                        tp = ExpType.Exp_Large;
                    }
                    caches = Node<Exp>.extend(
                        new Exp(tp, null, x, children, null),
                        caches
                    );
                    children = null;
                }
                else if (x.Token_type() == s.Token.TokenType.Token_BracketLeft)
                {
                    Exp cache = caches.First();
                    Node<Exp> r_children = null;
                    ExpType tp = cache.Exp_type();

                    if (tp == ExpType.Exp_Large)
                    {
                        check_Large(children);
                    }
                    else
                    {
                        r_children = Node<Exp>.reverse(children);
                    }
                    Node<Exp> caches_parent = caches.Rest();
                    if (caches_parent != null)
                    {
                        Exp p_exp = caches_parent.First();
                        if (p_exp.Exp_type() == ExpType.Exp_Large)
                        {
                            //�����ʽ�Ǻ���
                            if (tp == ExpType.Exp_Small)
                            {
                                if (children == null)
                                {
                                    throw new LocationException(x.Loc(),"������յ�()");/*������*/
                                }
                                else
                                {
                                    Exp first = children.First();
                                    if (first.Exp_type() == ExpType.Exp_Id && first.Value() == "let")
                                    {
                                        tp = ExpType.Exp_Let;
                                        if (children.Length() == 1)
                                        {
                                            throw first.exception("������յ�let���ʽ");/*let��ʶ��λ��*/
                                        }
                                        else
                                        {
                                            children = Node<Exp>.extend(children.First(), resetLetVKS(Node<Exp>.reverse(children.Rest())));
                                        }
                                    }
                                    else
                                    {
                                        if (!(first.Exp_type() == ExpType.Exp_Id || first.Exp_type() == ExpType.Exp_Large || first.Exp_type() == ExpType.Exp_Small))
                                        {
                                            throw first.exception("�������õ�һ��Ӧ����id��{}��()��������" + first.ToString());/*first�Ѿ����������*/
                                        }
                                    }
                                }
                            }
                        }
                    }

                    children =Node<Exp>.extend(
                        new Exp(
                            tp,
                            x,
                            cache.Right(),
                            children,
                            r_children
                        ),
                        cache.Children()
                    );
                    caches = caches_parent;
                }
                else
                {
                    ExpType tp=0;
                    bool deal = true;
                    if (x.Token_type() == s.Token.TokenType.Token_String)
                    {
                        tp = ExpType.Exp_String;
                    }
                    else if (x.Token_type() == s.Token.TokenType.Token_Int)
                    {
                        tp = ExpType.Exp_Int;
                    }
                    else if (x.Token_type() == s.Token.TokenType.Token_Bool)
                    {
                        tp = ExpType.Exp_Bool;
                    }
                    else
                    {
                        Exp parent = caches.First();
                        if (parent.Exp_type() == ExpType.Exp_Medium)
                        {
                            if (x.Token_type() == s.Token.TokenType.Token_Prevent)
                            {
                                if (x.Value() == "true" || x.Value() == "false" || s.Token.isInt(x.Value()))
                                {
                                    throw new LocationException(x.Loc(), "��������ת��Ѱ���������ϵ�" + x.Value() + "����");/*token���*/
                                }
                                tp = ExpType.Exp_Id;
                            }
                            else if (x.Token_type() == s.Token.TokenType.Token_Id)
                            {
                                tp = ExpType.Exp_String;
                            }
                            else
                            {
                                deal = false;
                            }
                        }
                        else
                        {
                            if (x.Token_type() == s.Token.TokenType.Token_Prevent)
                            {
                                tp = ExpType.Exp_String;
                            }
                            else if (x.Token_type() == s.Token.TokenType.Token_Id)
                            {
                                tp = ExpType.Exp_Id;
                            }
                            else
                            {
                                deal = false;
                            }
                        }
                    }

                    if (deal)
                    {
                        children =Node<Exp>.extend(
                            new Exp(tp, x),
                            children
                        );
                    }
                }
            }
            check_Large(children);
            return new Exp(ExpType.Exp_Large, root_left,root_right,children, null);
        }
    }
}
