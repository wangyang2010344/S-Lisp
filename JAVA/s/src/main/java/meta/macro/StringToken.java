package meta.macro;

import meta.*;

/**
 * 宏，将所有参数依空格相连
 */
public class StringToken extends LibReadMarco {
    @Override
    protected Object run(ScopeNode scope, Node<Exp> rest) throws Throwable {
        if (rest==null){
            return "";
        }else if (rest.length==1){
            if (rest.first instanceof IDExp){
                return rest.first.asIDExp().value;
            }else if(rest.first instanceof StringExp){
                return rest.first.asStringExp().value;
            }else{
                //括号表达式
                StringBuilder sb=new StringBuilder();
                join(rest.first,sb);
                return sb.toString();
            }
        }else{
            //非括号
            StringBuilder sb=new StringBuilder();
            for(Node<Exp> tmp=rest;tmp!=null;tmp=tmp.rest){
                join(tmp.first,sb);
                sb.append(" ");
            }
            sb.setLength(sb.length()-1);/*去除最后的空格*/
            return sb.toString();
        }
    }

    public static void join(Exp exp, StringBuilder sb){
        if (exp instanceof IDExp){
            sb.append(exp.asIDExp().value);
        }else{
            join_bracket((BracketExp)exp,sb);
        }
    }
    public static void join_bracket(BracketExp bracketExp,StringBuilder sb){
        sb.append("( ");
        for(Node<Exp> tmp=bracketExp.children;tmp!=null;tmp=tmp.rest){
            join(tmp.first,sb);
            sb.append(" ");
        }
        sb.append(")");
    }
}