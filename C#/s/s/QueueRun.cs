using System;
using System.Collections.Generic;
using System.Text;

namespace s
{
    public class QueueRun
    {
        private Node<Object> scope;
        public QueueRun(Node<Object> scope)
        {
            this.scope = scope;
        }
        /// <summary>
        /// ִ��
        /// </summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        public Object exec(Exp exp)
        {
            Object ret = null;
            for (Node<Exp> tmp = exp.Children(); tmp != null; tmp = tmp.Rest())
            {
                ret=run(tmp.First());
            }
            return ret;
        }
        Object run(Exp exp)
        {
            if (exp.Exp_type() == Exp.Exp_Type.Exp_Small)
            {
                if (exp.Children() != null)
                {
                    Exp t=exp.Children().First();
                    if (t.Exp_type() == Exp.Exp_Type.Exp_Id && t.Value() == "let")
                    {
                        Node<Exp> rst = exp.Children().Rest();
                        while (rst != null)
                        {
                            Exp key = rst.First();
                            rst = rst.Rest();
                            Exp value = rst.First();
                            rst = rst.Rest();
                            Object vas = interpret(value, scope);
                            scope = match(scope, key, vas);
                        }
                        return null;
                    }
                    else
                    {
                        return interpret(exp, scope);
                    }
                }else
                {
                    return interpret(exp, scope);
                }
            }
            else
            {
                return interpret(exp, scope);
            }
        }

        Node<Object> when_kvs_match(Node<Object> scope, String id, Object Value)
        {
            throw new Exception("Ϊ��ѩ�ص�kvs-math����ʱ��֧����*�Ž�β");
        }
        Node<Object> when_normal_match(Node<Object> scope, String id, Object Value)
        {
            return Node<object>.extend(id, Node<object>.extend(Value, scope));
        }

        static bool isWait(Exp exp)
        {
            if (exp.Exp_type() == Exp.Exp_Type.Exp_Id)
            {
                return exp.Value().StartsWith("...");
            }
            else
            {
                return false;
            }
        }
        Node<Object> when_bracket_match(Node<Object> scope, Node<Exp> keys, Node<Object> values)
        {
            while (keys != null)
            {
                Exp key = keys.First();
                Object value = null;
                if (keys.Rest() == null && isWait(key))
                {
                    String subvk = key.Value().Substring(3);
                    if (subvk[subvk.Length - 1] == '*')
                    {
                        scope = when_kvs_match(scope, subvk, values);
                    }
                    else
                    {
                        scope = when_normal_match(scope, subvk, values);
                    }
                }
                else
                {
                    if (values != null)
                    {
                        value = values.First();
                        values = values.Rest();
                    }
                    scope = match(scope, key, value);
                }
                keys = keys.Rest();
            }
            return scope;
        }
        Node<Object> match(Node<Object> scope, Exp key, Object value)
        {
            if (key.Exp_type() == Exp.Exp_Type.Exp_Id)
            {
                String id = key.Value();
                if (id[id.Length - 1] == '*')
                {
                    scope = when_kvs_match(scope, id, value);
                }
                else
                {
                    scope = when_normal_match(scope, id, value);
                }
            }
            else
            {
                if (key.Exp_type() == Exp.Exp_Type.Exp_Small)
                {
                    scope = when_bracket_match(scope, key.Children(), (Node<Object>)value);
                }
                else
                {
                    throw new Exception(key.Value() + "���ǺϷ�ƥ������");
                }
            }
            return scope;
        }
        Node<Object> calNode(Node<Exp> list, Node<Object> scope)
        {
            Node<Object> r = null;
            for (Node<Exp> x = list; x != null; x = x.Rest())
            {
                r = Node<object>.extend(
                    interpret(x.First(),scope),
                    r
                );
            }
            return r;
        }

        String getPath(Node<Object> scope)
        {
            String path = null;
            Node<Object> tmp = scope;
            while (tmp != null && path==null)
            {
                String key = tmp.First() as String;
                tmp = tmp.Rest();
                if (key == "pathOf")
                {
                    if (tmp.First() is Function)
                    {
                        Function pathOf = tmp.First() as Function;
                        path = pathOf.exec(null) as String;
                    }
                }
                tmp = tmp.Rest();
            }
            return path;
        }
        LocationException error_throw(String msg, Exp exp,Node<Object> scope,Node<Object> children)
        {
            LocationException locexp=new LocationException(exp.Loc(),  msg + ":\r\n"+children.ToString()+"\r\n"+ exp.Children().First() + "\r\n" + exp.Children().ToString());
            locexp.addStack(getPath(scope),exp.Loc(),exp.ToString());
            return locexp;
        }
        Object interpret(Exp exp, Node<Object> scope)
        {
            if (exp.Exp_type() == Exp.Exp_Type.Exp_Small)
            {
                Node<Object> children = calNode(exp.R_children(), scope);
                Object o=children.First();
                
                if (o is Function)
                {
                    try
                    {
                        return ((Function)o).exec(children.Rest());
                    }
                    catch (LocationException lex)
                    {
                        lex.addStack(getPath(scope), exp.Loc(), exp.ToString());
                        throw lex;
                    }
                    catch (Exception ex)
                    {
                        throw error_throw("����ִ���ڲ�����" + ex.Message, exp, scope, children);
                    }
                }
                else
                {
                    if (o == null)
                    {
                        throw error_throw("δ�ҵ���������", exp, scope, children);
                    }
                    else
                    {
                        throw error_throw("���Ǻ���", exp, scope, children);
                    }
                }
            }
            else if (exp.Exp_type() == Exp.Exp_Type.Exp_Medium)
            {
                return calNode(exp.R_children(), scope);
            }
            else if (exp.Exp_type() == Exp.Exp_Type.Exp_Large)
            {
                return new UserFunction(exp, scope);
            }
            else if (exp.Exp_type() == Exp.Exp_Type.Exp_String)
            {
                return exp.Value();
            }
            else if (exp.Exp_type() == Exp.Exp_Type.Exp_Int)
            {
                return int.Parse(exp.Value());
            }
            else if (exp.Exp_type() == Exp.Exp_Type.Exp_Id)
            {
                return Node<Object>.kvs_find1st(scope, exp.Value());
            }
            else
            {
                return null;
            }
        }
    }
}
