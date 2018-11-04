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

        private static Node<Object> kvs_extend(String key, Object value, Node<Object> kvs)
        {
            return Node<Object>.kvs_extend(key,value,kvs);
        }
        Node<Object> letSmallMatch(Exp small, Object v, Node<Object> scope)
        {
            Node<Exp> ks = small.Children();
            if (v == null || v is Node<Object>)
            {
                Node<Object> vs = v as Node<Object>;
                while (ks != null)
                {
                    v = null;
                    if (vs != null)
                    {
                        v = vs.First();
                    }
                    Exp k = ks.First();
                    ks = ks.Rest();

                    if (k.Exp_type() == Exp.ExpType.Exp_LetId)
                    {
                        scope = kvs_extend(k.Value(), v, scope);
                    }
                    else if (k.Exp_type() == Exp.ExpType.Exp_LetSmall)
                    {
                        scope = letSmallMatch(k, v, scope);
                    }
                    else if (k.Exp_type() == Exp.ExpType.Exp_LetRest)
                    {
                        scope =kvs_extend(k.Value(), vs, scope);
                    }
                    else
                    {
                        throw k.exception("�쳣ƥ��" + k.ToString());/*�Ѿ��ų��ˣ��������������*/
                    }
                    if (vs != null)
                    {
                        vs = vs.Rest();
                    }
                }
                return scope;
            }
            else
            {
                throw small.exception(v.ToString() + "���ǺϷ���List���ͣ��޷�����Ԫ��ƥ��:" + small.ToString());/*������ֵ�����б�*/
            }
        }
        Object run(Exp exp)
        {
            if (exp.Exp_type() == Exp.ExpType.Exp_Let)
            {
                Node<Exp> cs = exp.Children().Rest();
                while (cs != null)
                {
                    Exp key = cs.First();
                    cs=cs.Rest();
                    Object value = interpret(cs.First(), scope);
                    cs = cs.Rest();
                    if (key.Exp_type() == Exp.ExpType.Exp_LetId)
                    {
                        scope = kvs_extend(key.Value(), value, scope);
                    }
                    else if (key.Exp_type() == Exp.ExpType.Exp_LetSmall)
                    {
                        scope = letSmallMatch(key, value, scope);
                    }
                }
                return null;
            }
            else
            {
                return interpret(exp, scope);
            }
        }
        String getPath(Node<Object> scope)
        {
            String path = null;
            Node<Object> tmp = scope;
            while (tmp != null && path == null)
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
        /// <summary>
        /// ��������ʱ�׳�����
        /// </summary>
        /// <param name="scope"></param>
        /// <param name="msg"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        LocationException match_Exception(Node<Object> scope, String msg, Exp e)
        {
            return e.exception(getPath(scope) + ":\t" + msg);
        }
        /// <summary>
        /// ����ִ��ʱ�Ĵ���
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="exp"></param>
        /// <param name="scope"></param>
        /// <param name="children"></param>
        /// <returns></returns>
        LocationException error_throw(String msg, Exp exp,Node<Object> scope,Node<Object> children)
        {
            return exp.exception( msg + ":\r\n"+children.ToString()+"\r\n"+ exp.Children().First() + "\r\n" + exp.Children().ToString());
        }
        Object interpret(Exp exp, Node<Object> scope)
        {
            if (exp.Exp_type() == Exp.ExpType.Exp_Small)
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
                        lex.addStack(getPath(scope), exp.Left().Loc(),exp.Right().Loc(), exp.ToString());
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
            else if (exp.Exp_type() == Exp.ExpType.Exp_Medium)
            {
                return calNode(exp.R_children(), scope);
            }
            else if (exp.Exp_type() == Exp.ExpType.Exp_Large)
            {
                return new UserFunction(exp, scope);
            }
            else if (exp.Exp_type() == Exp.ExpType.Exp_String)
            {
                return exp.Value();
            }
            else if (exp.Exp_type() == Exp.ExpType.Exp_Int)
            {
                return exp.Int_Value();
            }
            else if (exp.Exp_type() == Exp.ExpType.Exp_Bool)
            {
                return exp.Bool_Value();
            }
            else if (exp.Exp_type() == Exp.ExpType.Exp_Id)
            {
                Node<String> paths = exp.KVS_paths();
                if (paths == null)
                {
                    throw match_Exception(scope, exp.Value() + "���ǺϷ���ID����:\t" + exp.ToString(), exp);
                }
                else
                {
                    Node<Object> c_scope = scope;
                    Object value = null;
                    while (paths != null)
                    {
                        String key = paths.First();
                        value = Node<Object>.kvs_find1st(c_scope, key);
                        paths = paths.Rest();
                        if (paths != null)
                        {
                            if (value == null || value is Node<Object>)
                            {
                                c_scope = value as Node<Object>;
                            }
                            else
                            {
                                throw match_Exception(scope, "����" + paths.ToString() + "������" + value + "����kvs����:\t" + exp.ToString(), exp);
                            }
                        }
                    }
                    return value;
                }
            }
            else
            {
                return null;
            }
        }
    }
}
