#pragma once
#include "./library/system.h"
namespace s{
    //为了支持控制台
    class QueueRun{
        bool iswait(Exp * e){
            if(e->exp_type()==Exp::Exp_Id){
                string &x=e->Value();
                if(x.size()>3){
                    if (x[0]=='.' && x[1]=='.' && x[2]=='.') {
                        return true;
                    }else{
                        return false;
                    }
                }else{
                    return false;
                }
            }else{
                return false;
            }
        }
        string getPath(Node * scope){
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
        Node* when_bracket_match(Node * scope,Node *keys,Node *values){
            while (keys!=NULL) {
                Exp * key=static_cast<Exp *>(keys->First());
                Base *value=NULL;
                if (keys->Rest()==NULL && iswait(key))
                {
                    /*
                    最后一个是匹配(可能还是kvs_match匹配，但不是bracket_match)
                    */
                    const string &vk=key->Value();
                    string subvk=vk.substr(3,vk.size());//不能是引用，只能是复制
                    scope=when_normal_match(scope,subvk,values,key->Loc());
                }else
                {
                    if(values!=NULL){
                        value=values->First();
                        values=values->Rest();
                    }
                    scope=match(scope,key,value);
                }
                keys=keys->Rest();
            }
            return scope;
        }
        LocationException *match_Exception(string msg,Location *loc,Node * scope)
        {
            LocationException* lex=new LocationException(getPath(scope)+":\t"+msg,loc);
            return lex;
        }

        Node * match(Node *scope,Exp *key,Base *value){
            //值为空，仍然需要增加定义
            if (key->exp_type()==Exp::Exp_Id) {
                string id=key->Value();
                scope=when_normal_match(scope,id,value,key->Loc());
            }else{
                if (key->exp_type()==Exp::Exp_Small) {
                    //括号匹配
                    Node* keys=static_cast<BracketExp*>(key)->Children();
                    if(value!=NULL){
                        value->retain();
                        scope=when_bracket_match(scope,keys,static_cast<Node *>(value));
                        value->release();
                    }else{
                        scope=when_bracket_match(scope,keys,NULL);
                    }
                }else{
                    throw match_Exception(key->Value()+"不是合法的类型",key->Loc(),scope);
                }
            }
            return scope;
        }
        /*vas可为空*/
        Node *when_normal_match(Node *scope,string & id,Base *vas,Location * loc){
            if(id.find('.')==string::npos){
                scope=kvs::extend(id,vas,scope);
            }else{
                throw match_Exception(id+"不是合法的key",loc,scope);
            }
            return scope;
        }
        Node * & scope;
        Base* run(Exp * e){
            if(e->exp_type()==Exp::Exp_Small){
                BracketExp *be=static_cast<BracketExp *>(e);
                if (be->Children()!=NULL) {
                    Exp *t=static_cast<Exp *>(be->Children()->First());
                    if (t->exp_type()==Exp::Exp_Id && t->Value()=="let") {
                        //let表达式
                        Node *rst=be->Children()->Rest();
                        while (rst!=NULL) {
                            Exp *key=static_cast<Exp*>(rst->First());
                            rst=rst->Rest();
                            Exp *value=static_cast<Exp*>(rst->First());
                            rst=rst->Rest();
                            Base * vas=interpret(value, scope);
                            scope=match(scope,key,vas);
                        }
                        return NULL;
                    }else{
                        return interpret(e, scope);
                    }
                }else{
                    return interpret(e, scope);
                }
            }else{
                return interpret(e, scope);
            }
        }
        /*(b a log)*/
        Node * calNode(Node * list,Node * scope)
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
        LocationException* call_exception(string msg,BracketExp * exp,Node * children,Node * scope)
        {
            msg=msg+"\n"+exp->toString()+"\n"+children->toString()+"\n";
            //cout<<"出现异常:"<<msg<<"在位置:"<<exp->Index()<<endl;
            children->release();
            /*
            scope->retain();
            scope->release();
            */
            return new LocationException(msg,exp->Loc());
        }
        Base * exec(Function* func,Node *rst,Node *children){
            /*函数的计算结果默认是+1的*/
            Base *b=func->exec(rst);
            children->release();
            if (b!=NULL) {
                //在计算结果时伪销毁。
                b->eval_release();
            }
            return b;
        }
        Base *interpret(Exp* e,Node * scope);
    public:
        QueueRun(Node * & scope):scope(scope){}
        Base * exec(BracketExp *exp){
            Base * ret=NULL;
            for (Node * tmp=exp->Children(); tmp!=NULL; tmp=tmp->Rest()) {
                if(ret!=NULL)
                {
                    //上一次的计算结果，未加到作用域
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
            Node * scope=kvs::extend("args",args,parentScope);
            scope=kvs::extend("this",this,scope);
            Base *ret=run(args, scope);
            if (ret!=NULL) {
                ret->retain();
            }
            //不能直接delete，可能被自定义函数闭包引用
            scope->retain();
            scope->release();
            //delete scope;
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
        Base * run(Node * args,Node * & scope){
            QueueRun qr(scope);
            return qr.exec(exp);
        }
    };
    Base *QueueRun::interpret(Exp* e,Node * scope){
        if(e->isBracket()){
            /*
             *scope可能作为父作用域，避免被销毁
             */
            scope->retain();
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
                    lex->addStack(getPath(scope),be->Loc(),be->toString());
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
            scope->eval_release();
            return b;
        }else
        if(e->exp_type()==Exp::Exp_String)
        {
            return new String(e->Value());
        }else
        if(e->exp_type()==Exp::Exp_Int)
        {
            return new Int(e->Value());
        }else
        if(e->exp_type()==Exp::Exp_Id)
        {
            IDExp * idexp=static_cast<IDExp*>(e);
            Node* paths=idexp->Paths();
            if(paths==NULL){
                throw match_Exception(idexp->Value()+"不是合法的ID类型",e->Loc(),scope);
            }else{
                Node * c_scope=scope;
                Base * value=NULL;
                while(paths!=NULL){
                    String* key=static_cast<String*>(paths->First());
                    value=kvs::find1st(c_scope,key->StdStr());
                    paths=paths->Rest();
                    if(paths!=NULL){
                        if(value==NULL || value->stype()==Base::sList){
                            c_scope=static_cast<Node*>(value);
                        }else{
                            throw match_Exception("计算"+paths->toString()+"出错，"+value->toString()+"不是kvs类型:\t"+e->toString(),e->Loc(),scope);
                        }
                    }
                }
                return value;
            }
            return kvs::find1st(scope,e->Value());
        }else{
            return NULL;
        }
    }
};
