'use strict';
var mb={};
var window={};
if((""+ini.get("engine_name"))=="Mozilla Rhino"){
    //Java7����Java��
    var Java={
	    type:function(str){
	        return eval("Packages."+str);
	    }
	};
    //ֻ֧��interface��
    mb.Java_new=function(cls,params,obj){
        if(typeof(cls)=='string'){
            cls=Java.type(cls);
        }
        return new JavaAdapter(cls,obj);
    };
}else{
    mb.Java_new=function(cls,params,obj){
        if(typeof(cls)=='string'){
            cls=Java.type(cls);
        }
        if(obj){
            var Class=Java.extend(cls,obj);
        }else{
            var Class=cls;
        }
        if(params&&params.length!=0){
	        var ps=[];
	        for(var i=0;i<params.length;i++){
	            ps.push("params["+i+"]");
	        }
            return eval("new Class("+ps.join(",")+");");
        }else{
            return new Class();
        }
    };
}
mb.Function=(function(){
    var quote=function(){
        return function(a){
            return a;
        }
    };
    quote.one=quote();
    var as_null=function(){
        return function(){
            return null;
        };
    };
    as_null.one=as_null();
    return {
        quote:quote,
        as_null:as_null
    };
})();
mb.log=(function(){
    var Stringify=function(o){
        if(typeof(o)=='object'){
            var ret={};
            for(var k in o){
                ret[k]=""+o[k];
            }
            return JSON.stringify(ret,"",2);
        }else{
            return ""+o;
        }
    };
    var loadder=function(prefix){
        var toPrint;
        var toLog;
        var System=Java.type("java.lang.System");
        var _print_=function(a){
            System.out.print(a);
        };
        if(prefix){
            toPrint=function(str){
                return prefix+":\t"+str;
            };
            toLog=function(){
                _print_(prefix+":");
                _print_("\t");
            };
        }else{
            toPrint=function(str){return str;};
            toLog=function(){};
        }
        var log=function(){
            toLog();
            for(var i=0;i<arguments.length;i++){
                _print_(arguments[i]);
                _print_('\t');
            }
            _print_('\n');
        };
        var common=function(key){
            return function(o){
                var str=Stringify(o);
                log(key,str);
                //����ж����������־�ļ���
                var sl=ini.get("log");
                if(sl){
                    sl[key](toPrint(str));
                }
            };
        };
        log.stringifyError=Stringify;
        log.info=common("info");
        log.warn=common("warn");
        log.error=common("error");
        log.debug=common("debug");
        return log;
    };
    var log=loadder();
    log.loadder=loadder;
    return log;
})();
mb.Exception=function(s){
    return new (Java.type("java.lang.Exception"))(s);
};
mb.Object={
	forEach:function(obj,func){
		for(var key in obj){
			func(obj[key],key);
		}
	},
	map:function(obj,func){
		var o={};
		for(var key in obj){
			o[key]=func(obj[key],key);
		}
		return o;
	}
};
mb.Array={
    toSet:function(arr){
        var ret={};
        arr.forEach(function(ar,i){
            ret[ar]=true;
        });
        return ret;
    },
    forEach:function(array,func){
        for(var i=0;i<array.length;i++){
            func(array[i],i);
        }
    },
    isArray:function(array){
        return Array.isArray(array);
    },
    map:function(array,func){
        var ret=[];
        for(var i=0;i<array.length;i++){
            ret[i]=func(array[i],i);
        }
        return ret;
    }
};
mb.time=(function(){
	var cache={};
	var getfmt=function(fmt_key){
		var fmt=cache[fmt_key];
		if(!fmt){
			fmt=new (Java.type("java.text.SimpleDateFormat"))(fmt_key);
			cache[fmt_key]=fmt;
		}
		return fmt;
	};
	return {
		now:function(fmt_key){
			return ""+getfmt(fmt_key).format(new (Java.type("java.util.Date"))());
		},
        from_millis:function(mills,fmt_key){
            var cal=Java.type("java.util.Calendar").getInstance();
            cal.setTimeInMillis(mills);
            return ""+getfmt(fmt_key).format(cal.getTime());
        }
	};
})();
mb.exist=function(v){
	return (v!=null && v!="")
};
mb.load=(function(){
    var sp=ini.get("file_sp");
    var base_path=ini.get("server_path")+sp;
    var me=ini.get("me");
    

    /*�������·��*/
    var calAbsolutePath=function(base_url,url){
        var base=base_url.substr(0,base_url.lastIndexOf('/'))+'/'+url;
        var nodes=base.split('/');
        var rets=[];
        var last=null;
        for(var i=0;i<nodes.length;i++){
            var node=nodes[i];
            if(node=='..'){
                if(last=='..'){
                    rets.push(node);
                }else
                if(last==null){
                    rets.push(node);
                    last=node;
                }else{
                    rets.pop();
                    last=rets[rets.length-1];
                }
            }else
            if(node=='.'){
                //����
            }else{
                rets.push(node);
                last=node;
            }
        }
        return rets.join('/');
    };
    
    var pkgLoader=function(path){
        //��Ҫ�뵥�����صı���һ��
        var me={
            relative_to_absolute:function(relative){
                return mb.load.path(mb.load.calAbsolutePath(path,relative));
            },
            current_act:function(){
                return path;  
            },
            load_file:function(relative){
                return mb.load.file(mb.load.calAbsolutePath(path,relative));
            },
            log:mb.log.loadder(path)
        };
        return me;
    };
    return {
	    text:function(path){
	        return me.readTxt(base_path+path.replace(/>/g,sp));
	    },
        /**
         * ����JAVA��Fileʵ��������jsΪ�����κ�·����
         */
	    file:function(path){
	        return me.fileFromPath(base_path+sp+path+sp)
	    },
        path:function(path){
            return base_path+sp+path;
        },
        calAbsolutePath:calAbsolutePath,
        pkgLoader:pkgLoader,
	    require:(function(){
        var fakemap=function(){
            var _c={};
            return {
                get:function(key){
	                 return _c[key];
	            },
	            put:function(key,value){
	                 _c[key]=value;
	            },
                clear:function(){
                    _c={};
                }
	        };
	    };
	    var core=fakemap();
		//var core=ini.get("js_cache");
		/**
		 * data
		 * success
		 */
		var ret=function(p){
			var me={
				retData:null,//ע��
				url:null,//ע��
				init:function(){
					if(p.data){
						mb.Object.forEach(p.data, function(value,key){
							me.retData[key]=ret.sync(value,me.url)();
						});
					}
				},
                delay:p.delay
			};
            if(typeof(p.success)=='function'){
                me.success=function(){
                    try{
                        return p.success.apply(null,arguments);
                    }catch(e){
                        mb.log("����:",me.url);
                        throw e;
                    }
                };
            }else{
                me.success=p.success;
            }
			return me;
		};
        ret.core=core;
		ret.sync=function(value,base_url){
            if(value[0]=='.'){
                value=calAbsolutePath(base_url,value);
            }
			var vk=core.get(value);
			if(!vk){
				//mb.log("new:"+value);
				var txt=mb.load.text(value);
                if(txt==""){
                    return null;
                }else{
					var lib={};
                    //pkg������Ҫ�Գ�
                    var pkg=pkgLoader(value);
					var cvk=eval("mb.load.require"+txt);
					cvk.retData=lib;
					cvk.url=value;
					cvk.init();
                    var s;
                    if(cvk.delay){
                        s=cvk.success();
                    }else{
                        s=cvk.success;
                    }
                    /**������һ���ļ����������*/
                    vk=function(){
                        return s;
                    };
					core.put(value,vk);
                }
			};
			return vk;
	    };
	    return ret;
	    })()
	};
})();
mb.compile=(function(){
    //importPackage(java.io);
    var sp=""+ini.get("file_sp");
    var base_path=""+ini.get("server_path");
    
    var escapeStr=function(str){
        return "\""+str.replace(/"/g,"\\\"")+"\"";
    };
    var stringify=function(p){
        var tp=typeof(p);
        if(tp=='function' || tp=="boolean" || tp=="number"){
            return p.toString().trim();
        }else
        if(p==null)
        {
            return "null";
        }else
        if(tp=="string"){
            return escapeStr(p);
        }else
        if(p instanceof Array){
            var ret=[];
            for(var i=0;i<p.length;i++){
                ret.push(stringify(p[i]));
            }
            return '['+ret.join(',')+']';
        }else
        {
            //object
            var ret=[];
            for(var k in p){
                ret.push(escapeStr(k)+":"+stringify(p[k]));
            }
            return '{'+ret.join(',')+'}';
        }
    };
    /**
     * �����ļ�
     */
    var saveTxt=function(content,path){
    	ini.get("me").saveText(content,path);
    };
    var loadTxt=function(f){
        return ini.get("me").readTxt(f);
    };
    /**
     * ��mb.load.requireд���ļ�������������ⶼ����д���롣��Ϊ��toString�Ժ���ת�����������Ч��
     * ���ʽ����ֱ��ʹ�ÿ⣬��Ϊ���ñ��ʽͬ����ʱʹ��eval���Ѿ������ʽ���򣬱��뱣��Ϊ�������ֵ����һ�ṹ��
     * 
     * ��mb.ajax.require����ʾ��
     * 1��require������Ĵ�����Ч��������Ϊ�޷�ʹ�ÿ⣬����û�����塣
     * 2��success��ʹ�������ֵ䣬Ҳ����ʹ�ÿ⣬ֻҪ���ÿ��Ҷ�ӽڵ��Ǻ�����
     * 3����delay��ʹ�ÿ⣬�������κα��ʽ������ִ�еĺ���
     * 4����ʹ�����֣�ֻʹ��({})������
     * 
     * ��ʵ���ظ���������⣬ÿ�η��صĺ�������һ�������Ի���һ�¡�
     * ����delay��ѭ���������⣬����ʱ������δ���������ֹ���ʱ�������ʱ�⡣
     */
    var loadRequire=(function(){
        var require=function(p){
            return p;
        };
        var regx=(function(){
            if('/'==sp){
                return /\//g;
            }else
            if('\\'==sp){
                return /\\/g;
            }else
            {
                return eval("/"+sp+"/g");
            }
        })();
        var circleLoad=function(parent,name,root){
            var children=parent.listFiles();
            if(children==null)return;
            for(var i=0;i<children.length;i++){
                var child=children[i];
                var childName=name+child.getName();
                if(child.isDirectory()){
                    circleLoad(child,childName+sp,root);
                }else
                {
                    var suffix = childName.substring(childName.lastIndexOf(".") + 1).toLowerCase();
                    if("js"==suffix){
                        root[childName.replace(regx,'/')]="require"+loadTxt(child);
                    }
                }
            }
        };
        var singLoad=function(folderName,root){
            circleLoad(mb.load.file(folderName),folderName,root);
        };
        return function(){
            var root={};
            for(var i=0;i<arguments.length;i++){
                singLoad(arguments[i],root);
            }
            return  ["(function(){",
                    "var calAbsolutePath=mb.load.calAbsolutePath;",
                    "var pkgLoader=mb.load.pkgLoader;",
                    "var xload="+(function(obj){
                        var ret={};
                        for(var key in obj){
                            ret[key]=core[obj[key]]();
                        }
                        return ret;
                    }).toString(),
                    "  var cache={};",
                    "  var core={};",
                    "  var servlets={};",
                    (function(){
                            var core=[];
                            for(var key in root){
                            	try{
                            		var obj=eval(root[key]);
                            	}catch(ex){
                            		mb.log("����"+key);
                            		mb.log(mb.log.stringifyError(ex));
                            	}
                                var escapeKey=escapeStr(key);
                                var newFunc=["function(){ ",
                                			"    var  ret=cache["+escapeKey+"];", //����һ�£�ִֻ��һ��
                                			"    if(!ret){",
                                            "    var pkg=pkgLoader("+escapeKey+");",
                                            "    var lib=xload("+JSON.stringify(
                                            		mb.Object.map(obj.data||{},function(val){
                                            			val=val.trim();
                                            			if(val[0]=='.'){
                                            				val=mb.load.calAbsolutePath(key,val);
                                            			}
                                                        if(!root[val]){
                                                            mb.log("δ�ҵ�\""+val+"\"��"+escapeKey);
                                                        }
                                                        return val;
                                            		})
                                            ,"",2)+");",
                                            "    ret="+stringify(obj.success).trim()+(obj.delay?"()":"")+";",
                                            "		 cache["+escapeKey+"]=ret;",
                                            "    }",
                                            "	 return ret;",
                                            "};"].join('\r\n');
                                core.push("core["+escapeKey+"]="+newFunc);
                                if(obj.out){
                                    //�����out��ʶ���ɹ�servlet����
                                    core.push("servlets["+escapeKey+"]=core["+escapeKey+"];");
                                }
                            }
                            return core.join(";\r\n");
                    })(),
                    "  return {coreData:core,servlets:servlets};",
                    "})()"].join('\r\n');
        };
    })();
    
    var loadMB=function(array,name){
         array.push(loadTxt(base_path+sp+"mb"+sp+name));
    };
    /**
     * ����������ֵ�
     */
    var ret=function(){
        
		var ret=["'use strict';"];
		//�������
		loadMB(ret,"lib.js");
		//���ݰ�
		loadMB(ret,ini.get("engine_name")+".js");
		//��Ŀ���԰�
		loadMB(ret,"common.js");
        if(ini.get("package")){
            //�����һ���ļ�
            var rqs=loadRequire(
                "act"+sp,
                "util"+sp,
                "ext"+sp);
            ret.push("(");
            ret.push(
                (function(xp){/*{coreData,servlets}*/
                    var getLib=function(libs,path){
                        var lib=libs[path];
                        if(lib){
                            return lib();
                        }else{
                            return null;
                        }
                  };
                  //�����
                  getLib(xp.coreData,"ext/index.js");
                  if(ini.get("servlet")==true){
                       mb.init(function(path){return getLib(xp.servlets,path);});
                  }else{
                       mb.init(function(path){return getLib(xp.coreData,path);});
                  }
                }).toString()
           );
           ret.push(")");
           ret.push("("+rqs+")");
       }else{
           //������ļ�
           ret.push("(");
           ret.push(
               (function(){
                    var getLib=function(c_path){
			            var lib=mb.load.require.sync(c_path);
			            if(lib){
			                return lib();
			            }else{
			                return null;
			            }
			        };
                    
                    //�����
                    getLib("ext/index.js");
                    mb.init(getLib);
               }).toString()
           );
           ret.push(")");
           ret.push("()");
        }
        return ret.join('\r\n');
    };
    //���浽�ļ�
    ret.save=function(){
        var x=ret();
        saveTxt(x,base_path+sp+"out.jsx");
        return x;
    };
    return ret;
})();

/**
 * java7���ݿ�
 */


/**
 * ��Ŀ���Կ�
 */

mb.request=(function(){
	var get=function(key){
		return ini.get("request").get(key);
	};
	return {
	    get:function(key){
	        var ret=get(key);
	        if(ret==null){
	            return "";
	        }else{
	            return ""+ret;
	        }
	    },
	    json:function(key){
	    	var ret=get(key);
	    	if(ret!=null){
	    		return JSON.parse(ret);
	    	}else{
	    		return null;
	    	}
	    }
	};
})();
mb.response=(function(){
    var write=function(code,description,obj){
        var res=ini.get("response");
        res.put("code",code);
        res.put("description",description);
        res.put("data",obj);
    };
    return {
        object:write,
        json:function(obj){
            write(0,"�����ɹ�",JSON.stringify(obj));
        },
        success:function(msg){
            write(0,""+(msg||"�����ɹ�"));
        },
        error:function(msg,code){
            write(code||-1,""+msg);
        }
    };
})();
mb.init=(function(){
    if(ini.get("servlet")==true){
        var initFunc=function(succ){
	        /**
	         * ��ʽ�ĵ���JS
	         */
	        //��servletֱ�ӵ��õ����
	        var req=ini.get("request");
	        var res=ini.get("response");
	        var servlet=mb.servlet(req,res);
	        var request=(function(){
	            var url=(""+req.getRequestURI());
	            var url_prefix=""+ini.get("url_prefix");
	            var act=url.substring(url.indexOf(url_prefix)+url_prefix.length,url.length);
	            act=""+decodeURI(act);
	            if(url_prefix!="/"){
	                var log_prefix=url_prefix.substring(0,url_prefix.indexOf("/"));
	                ini.put("log",ini.get("me").getLogger(log_prefix));
	            }
	            var me={
	                getAct:function(){
	                    return act;
	                },
	                session:servlet.session //��ͬ��Ŀ��ͬʵ��
	            };
	            var parseJSON=function(str){
	                return JSON.parse(str.replace(/&quot;/g,"\""));
	            };
	            me.p_str=function(key){
	                var v=req.getParameter(key);
	                if(v){
	                    return ""+v;
	                }else{
	                    return null;
	                }
	            };
	            me.p_strs=function(key){
	                //���ض�ά����
	                var vs=req.getParameterValues(key);
	                var ret=[];
	                for(var i=0;i<vs.length;i++){
	                    ret.push(""+vs[i]);
	                }
	                return ret;
	            };
	            me.p_json=function(key){
	                var v=me.p_str(key);
	                if(v==null){
	                    return v;
	                }else{
	                    return parseJSON(v);
	                }
	            };
	            me.p_jsons=function(key){
	                var vs=me.p_strs(key);
	                for(var i=0;i<vs.length;i++){
	                    vs[i]=parseJSON(vs[i]);
	                }
	                return vs;
	            };
	            servlet.request(me);//��չʵ��
	            return me;
	        })();
	        
	        var response=(function(){
	            var w=res.getWriter();
	            var isWrite=false;
	            var write=function(txt){
	                isWrite=true;
	                res.setHeader("Content-type", "text/html;charset=UTF-8");
	                res.setCharacterEncoding("utf-8");
	                w.append(txt);
	            };
	            var writeJSON=function(obj){
	                write(JSON.stringify(obj));
	            };
	            var me={
	                json:function(o){
	                    writeJSON({
	                        code:0,
	                        description:"�����ɹ�",
	                        data:o
	                    });
	                },
	                success:function(msg){
	                    writeJSON({
	                        code:0,
	                        description:msg||"�����ɹ�"
	                    });
	                },
	                error:function(msg,code){
	                    writeJSON({
	                        code:code||-1,
	                        description:msg
	                    });
	                },
	                text:function(txt){
	                    write(txt);
	                },
	                isWrite:function(){
	                    return isWrite;
	                }
	            };
	            //��չʵ��
	            servlet.response(me,function(v){
	                isWrite=v;
	            })
	            return me;
	        })();
	        /**
	         * ΪʲôҪ��request/response��Ϊ�����Ĳ������ݶ�����ȫ�ֵĳ�����
	         * δ����������js���Ǵ����̣߳�����web������Ķ��̡߳�
	         * request������session�����Բ�ѯ����Ա��Ȩ��
	         * web��������״̬��
	         */
	        var doServlet=function(servlet,str){
	            var will=true;
	            servlet(request,response);
	            if(response.isWrite()){
	                will=false;
	            }
	            return will;
	        };
            var path=request.getAct();
            var nodes=path.split("/");
            var substr="";
            var will=true;
            var has=false;
            var already=[];
            var work_base_path=ini.get("work_base_path");
            if(work_base_path!=""){
                work_base_path="/"+work_base_path;
            }
            already.push("act"+work_base_path);
            try{
                while(nodes.length!=0 && will){
                    already.push(nodes.shift());
                    var substr=already.join("/");
                    /**
                     * //�Ȳ��Ҵ���Ŀ¼���ļ�������У�һ�㶼���ü������ң���Ϊ�����ļ��в���Ҷ���ļ���
                     */
                    var servlet=succ(substr+"/_.js");
                    if(servlet){
                        /**
                         * �����ǹ���������Ϊ֦�ڵ㣬�����޴���ת����һ������
                         */
                        will=doServlet(servlet);
                    }else{
                        /**
                         * Ҷ�ӽ�㣬��������ļ���ͬ������ô�죿�е���ű�ͬ���ļ������˵�ζ��������ͬ���ļ����ļ��в�����ͻ����Ϊ��js��׺��ͬ���ļ���Ӧ����Ϊ����㡣
                         */
                        servlet=succ(substr+".js");
                        if(servlet){
                            has=true;
                            will=doServlet(servlet);
                        }
                    }
                }
                if(will){
                    //û���ҵ�
                    if(has){
                        mb.log.error("��������ҳ�棬����δ����");
                    }
                    //�й�Ĭ�ϵĴ������������Ǹ������������ǲ����ذɣ�����404����δ����
                    response.error("δ�ҵ���Ӧ����ķ���",404);
                }
            }catch(ex){
                mb.log.error(ex);
                response.error(""+ex,500);
            }
        };
    }else{
        /**
         * ��ͳ�ļ�ӵ���JS
         */
	    var initFunc=function(succ){
	        var act=""+ini.get("act");
	        if(act!=""){
	            var path=("act>"+act+".js").replace(/>/g,"/");
                var action= succ(path);
                var e=null;
                if(action){
                    try{
                        action();
                    }catch(ex){
                        e=ex;
                    }
                }else{
                    e="δ�ҵ�ָ��ҳ��"+path;
                }
                if(e){
                    mb.log(act,e,"����");
                    mb.log.error(e);
                    mb.response.error(e,404);
                }
	        }
	    };
    }
    return initFunc;
})();

(
function(){
                    var getLib=function(c_path){
			            var lib=mb.load.require.sync(c_path);
			            if(lib){
			                return lib();
			            }else{
			                return null;
			            }
			        };
                    
                    //�����
                    getLib("ext/index.js");
                    mb.init(getLib);
               }
)
()