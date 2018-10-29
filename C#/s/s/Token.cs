using System;
using System.Collections.Generic;
using System.Text;

namespace s
{
    public class Token
    {
        public enum Token_Type { 
            Token_BracketLeft,
            Token_BracketRight,
            Token_Comment,
            Token_Prevent,
            Token_String,
            Token_Id,
            Token_Int,
            Token_Bool
        }
        private String value;
        private String old_value;
        public String Old_Value()
        {
            return old_value;
        }
        private Token_Type type;
        private Location loc;
        public Token(String value,String old_value, Token_Type type, Location loc) {
            this.value = value;
            this.old_value = old_value;
            this.type = type;
            this.loc = loc;
        }

        public String Value() 
        {
            return value;
        }
        public Token_Type Token_type()
        {
            return type;
        }
        public Location Loc() {
            return loc;
        }

        public override string ToString()
        {
            return old_value;
        }

        static bool isBlank(char c)
        {
            return (c == ' ' || c == '\t' || c == '\r' || c == '\n');
        }
        static bool notNumber(char c)
        {
            return (c < '0'|| '9' < c) ;
        }
        static bool isQuoteLeft(char c)
        {
            return (c == '(' || c == '[' || c == '{');
        }
        static bool isQuoteRight(char c)
        {
            return (c == '}' || c == ']' || c == ')');
        }
        public static bool isInt(String id)
        {
            bool ret=true;
            for(int i=0;i<id.Length;i++)
            {
                char c=id[i];
                if(notNumber(c)){
                    ret=false;
                }
            }
            return ret;
        }

        static Token deal_id(Code code, Location loc)
        {
            int start = loc.Index();
            String Id = code.substr(start, code.index());
            Token token;
            if (Id[0] == '\'')
            {
                if (Id.Length == 1)
                {
                    throw new LocationException(loc,"����'������");
                }
                else
                {
                    token = new Token(Id.Substring(1),Id, Token_Type.Token_Prevent, loc);
                }
            }else if (isInt(Id))
            {
                token = new Token(Id,Id, Token_Type.Token_Int, loc);
            }
            else if (Id == "true" || Id=="false")
            {
                token = new Token(Id,Id, Token_Type.Token_Bool, loc);
            }else
            {
                token = new Token(Id,Id, Token_Type.Token_Id, loc);
            }
            return token;
        }

        static Node<Token> tokenize_split(Code code,Node<Token> tokens,Token_Type type,Location loc,char end)
        {
            bool unbreak = true;
            int start = code.index();
            int trans_time = 0;
            while (code.noEnd() && unbreak) {
                char c = code.current();
                if (c == end)
                {
                    String str = code.substr(start, code.index());
                    if (trans_time != 0)
                    {
                        str = Util.stringFromEscape(str, end, trans_time);
                    }
                    tokens = Node<Token>.extend(
                        new Token(str,code.substr(start-1,code.index()+1),type,loc),
                        tokens
                    );
                    code.shift();
                    unbreak = false;
                }
                else
                {
                    if (c == '\\')
                    {
                        trans_time++;
                        code.shift();
                    }
                    code.shift();
                }
            }
            if (unbreak)
            {
                throw new LocationException(loc, "������Χ��δ����");
            }
            return tokens;
        }
        static Node<Token> tokenize_ID(Code code,Location loc, Node<Token> rest)
        {
            bool unbreak = true;
            while (code.noEnd() && unbreak)
            {
                char c = code.current();
                if (!(isBlank(c) || isQuoteLeft(c) || isQuoteRight(c) || c=='"' || c=='`'))
                {
                    code.shift();
                }
                else
                {
                    Token token = deal_id(code, loc);
                    rest=Node<Token>.extend(token,rest);
                    unbreak=false;
                }
            }
            if(unbreak)
            {
                Token token=deal_id(code,loc);
                rest=Node<Token>.extend(token,rest);
            }
            return rest;
        }
        public static Node<Token> run(String txt, char lineSplit)
        {
            Code code = new Code(txt, lineSplit);
            Node<Token> rest = null;
            while (code.noEnd())
            {
                char c = code.current();
                if(isBlank(c))
                {
                    code.shift();
                }else if (isQuoteLeft(c))
                {
                    String cs=""+c;
                    rest = Node<Token>.extend(new Token(cs,cs, Token_Type.Token_BracketLeft, code.currentLoc()), rest);
                    code.shift();
                }else if (isQuoteRight(c))
                {
                    String cs=""+c;
                    rest = Node<Token>.extend(new Token(cs,cs, Token_Type.Token_BracketRight, code.currentLoc()), rest);
                    code.shift();
                }else if(c=='"')
                {
                    Location loc = code.currentLoc();
                    code.shift();
                    rest=tokenize_split(code,rest,Token_Type.Token_String,loc,'"');
                }
                else if (c == '`')
                {
                    Location loc = code.currentLoc();
                    code.shift();
                    rest =tokenize_split(code, rest, Token_Type.Token_Comment,loc, '`');
                }
                else
                {
                    rest = tokenize_ID(code, code.currentLoc(), rest);
                }
            }
            return rest;
        }
    }
}
