#pragma once
#include "./library/system.h"
namespace s{
    //为了支持控制台
    class QueueRun{
        static string getPath(Node * scope){
            string path="";
            Node* tmp=scope;
            while(tmp!=NULL && path==""){
                String * key=static_cast<String*>(tmp->First());
                tmp=tmp->Rest();
                if("pathOf"==key->StdStr()){
                    if(tmp->First()->stype()==Base::sFunction){
                        Function * pathOf=static_cast<Function*>(tmp->First());
                        String* Path=static_cast<String*>(pathOf->exec(NULL));
                        path=Path->StdStr();
                        Path->release();
                    }
                }
                tmp=tmp->Rest();
            }
            return path;
        }

        static LocationException *match_Exception(string msg,Exp *e,Node * scope)
        {
            return e->exception(getPath(scope)+":\t"+msg);
        }

        /*每次增加1*/
        static Node* kvs_extend(String* key,Base* value,Node* scope){
            Node* n_scope=kvs::extend(key,value,scope);
            scope->eval_release();/*不需要检查销毁，因为不会销毁*/
            n_scope->retain();
            return n_scope;
        }
        static Node * letSmallMatch(BracketExp * small,Base * v,Node * scope){
            Node * ks=small->Children();
            if(v==NULL || v->stype()==Base::sList){
                Node * vs=static_cast<Node*>(v);
                while(ks!=NULL){
                    v=NULL;
                    if(vs!=NULL){
                        v=vs->First();
                    }
                    Exp * k=static_cast<Exp*>(ks->First());
                    ks=ks->Rest();

                    if(k->exp_type()==Exp::Exp_LetId){
                        scope=kvs_extend(static_cast<AtomExp*>(k)->Value(),v,scope);
                    }else
                    if(k->exp_type()==Exp::Exp_LetSmall){
                        scope=letSmallMatch(static_cast<BracketExp*>(k),v,scope);
                    }else
                    if(k->exp_type()==Exp::Exp_LetRest){
                        scope=kvs_extend(static_cast<AtomExp*>(k)->Value(),vs,scope);
                    }
                    if(vs!=NULL){
                        vs=vs->Rest();
                    }
                }
                return scope;
            }else{
                throw small->exception(v->toString()+"不是合法的List类型，无法参与元组匹配:"+small->toString());
            }
        }

        static Node * match(Exp * key,Base * value,Node * scope){
            if(key->exp_type()==Exp::Exp_LetId){
                scope=kvs_extend(static_cast<AtomExp*>(key)->Value(),value,scope);
            }else
            if(key->exp_type()==Exp::Exp_LetSmall){
                scope=letSmallMatch(static_cast<BracketExp*>(key),value,scope);
            }else{
                throw key->exception("尚不支持的Let-key类型"+key->toString());
            }
            return scope;
        }
        Node * scope;
        Base* run(Exp * e){
            if(e->exp_type()==Exp::Exp_Let){
                BracketExp *be=static_cast<BracketExp *>(e);
                Node * cs=be->Children()->Rest();
                while(cs!=NULL){
                    Exp * key=static_cast<Exp*>(cs->First());
                    cs=cs->Rest();
                    Base * value=interpret(static_cast<Exp*>(cs->First()),scope);
                    cs=cs->Rest();

                    if(value!=NULL){
                        value->retain();
                        scope=match(key,value,scope);
                        value->release();
                    }else{
                        scope=match(key,NULL,scope);
                    }
                }
                return NULL;
            }else{
                return interpret(e, scope);
            }
        }
        /*(b a log)*/
        static Node * calNode(Node * list,Node * scope)
        {
            Node * r=NULL;
            for(Node * x=list;x!=NULL;x=x->Rest())
            {
                Exp *xe=static_cast<Exp *>(x->First());
                Base *xv=interpret(xe,scope);
                r=new Node(xv,r);
            }
            return r;
            //return list::reverseAndDelete(r);
        }
        static LocationException* call_exception(string msg,BracketExp * exp,Node * children,Node * scope)
        {
            msg=msg+"\n"+exp->toString()+"\n"+children->toString()+"\n";
            //cout<<"出现异常:"<<msg<<"在位置:"<<exp->Index()<<endl;
            children->release();
            /*
            scope->retain();
            scope->release();
            */
            return exp->exception(msg);
        }
        static Base * exec(Function* func,Node *rst,Node *children){
            /*函数的计算结果默认是+1的*/
            Base *b=func->exec(rst);
            children->release();
            if (b!=NULL) {
                //在计算结果时伪销毁。
                b->eval_release();
            }
            return b;
        }
        static Base *interpret(Exp* e,Node * scope);
    public:
        QueueRun(Node * scope){
            this->scope=scope;
            scope->retain();
        }
        ~QueueRun(){
            scope->release();
        }
        Base * exec(BracketExp *exp){
            Base * ret=NULL;
            for (Node * tmp=exp->Children(); tmp!=NULL; tmp=tmp->Rest()) {
                if(ret!=NULL)
                {
                    /*上一次的计算结果，未加到作用域*/
                    /*
                        如果表达式中有函数
                        作用域作为此函数的父作用域，此函数在下一次销毁，则本作用域会销毁
                        所以let表达式必须每次增加1，下一次追加时减1
                    */
                    ret->retain();
                    ret->release();
                }
                Exp *e=static_cast<Exp *>(tmp->First());
                ret=this->run(e);
            }
            return ret;
        }
    };
    class UserFunction:public Function{
    public:
        UserFunction(BracketExp * exp,Node * parentScope):Function(){
            this->exp=exp;
            this->exp->retain();
            this->parentScope=parentScope;
            //作用域必须有，所以不需要检查空指针。
            this->parentScope->retain();
        }
        virtual Base *exec(Node *args)
        {
            Node * scope=kvs::extend(Function::S_args(),args,parentScope);
            scope=kvs::extend(Function::S_this(),this,scope);
            QueueRun qr(scope);//结束时自动销毁
            Base *ret=qr.exec(exp);
            if (ret!=NULL) {
                ret->retain();
            }
            return ret;
        }
        virtual ~UserFunction(){
            this->exp->release();
            this->parentScope->release();
        }
        Fun_Type ftype(){
            return Function::fUser;
        }
        string toString(){
            return exp->toString();
        }
    protected:
        Node * parentScope;
        BracketExp * exp;
    };
    Base *QueueRun::interpret(Exp* e,Node * scope){
        if(e->isBracket()){
            Base *b;
            BracketExp * be=static_cast<BracketExp*>(e);
            if(be->exp_type()==Exp::Exp_Small)
            {
                //小括号
                Node * children=calNode(be->R_children(),scope);
                children->retain();
                Base* first=children->First();
                Function * func=static_cast<Function*>(first);
                Node * rst=children->Rest();
#ifdef DEBUG
                /*
                    深思熟虑，能正常执行，
                    不处理异常，测试时有异常有内存泄漏，
                    测试过了是没有的，
                    异常处理方方面面，比较麻烦
                */
                if(func==NULL)
                {
                    //没法销毁函数内的引用计数
                    throw call_exception("未找到函数定义",be,children,scope);
                }
                try{
                    b=exec(func,rst,children);
                }catch(LocationException *lex){
                    lex->addStack(getPath(scope),be->Left()->Loc(),be->Right()->Loc(),be->toString());
                    throw lex;
                }catch(string & err_str){
                    throw call_exception(err_str,be,children,scope);
                }catch(...){
                    //无法捕获到，怎么处理？
                    throw call_exception("调用出错",be,children,scope);
                }
#else
                b=exec(func,rst,children);
#endif
            }else
            if(be->exp_type()==Exp::Exp_Medium)
            {
                //中括号
                b=calNode(be->R_children(),scope);
            }else
            if(be->exp_type()==Exp::Exp_Large)
            {
                //大括号
                b=new UserFunction(be,scope);
            }else{
                b=NULL;
            }
            return b;
        }else{
            AtomExp* ae=static_cast<AtomExp*>(e);
            if(ae->exp_type()==Exp::Exp_String)
            {
                return ae->Value();
            }else
            if(ae->exp_type()==Exp::Exp_Int)
            {
                return static_cast<IntExp*>(ae)->Int_Value();
            }else
            if(ae->exp_type()==Exp::Exp_Bool){
                return static_cast<BoolExp*>(ae)->Bool_Value();
            }else
            if(ae->exp_type()==Exp::Exp_Id)
            {
                IDExp * idexp=static_cast<IDExp*>(ae);
                Node* paths=idexp->Paths();
                if(paths==NULL){
                    throw match_Exception(idexp->Value()->StdStr()+"不是合法的ID类型",e,scope);
                }else{
                    Node * c_scope=scope;
                    Base * value=NULL;
                    while(paths!=NULL){
                        String* key=static_cast<String*>(paths->First());
                        value=kvs::find1st(c_scope,key);
                        paths=paths->Rest();
                        if(paths!=NULL){
                            if(value==NULL || value->stype()==Base::sList){
                                c_scope=static_cast<Node*>(value);
                            }else{
                                throw match_Exception("计算"+paths->toString()+"出错，"+value->toString()+"不是kvs类型:\t"+e->toString(),e,scope);
                            }
                        }
                    }
                    return value;
                }
            }else{
                return NULL;
            }
        }
    }
};
