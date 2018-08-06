# S-Lisp

## 项目介绍
一种Lisp脚本方言，主要是自己构思的一套语法方案。

貌似大家都没有gitee的账号，看来只有转到[github](https://github.com/wangyang2010344/S-Lisp)。

大家好

这是我自己研究的一套Lisp语法方案，暂称为S-Lisp。
从red获得过灵感，参考过【[lisp的根源](http://daiyuwen.freeshell.org/gb/rol/roots_of_lisp.html)】这篇文章，当然更主要的是js与lisp。

这种方案的最大特点是简单，小括号()是传统的lisp函数调用，中括号[]是列表，大括号{}是函数。
> 函数也是一种值节点

```
`简单示例`
>>
(let forEach
	{
		(let (vs run) args forEach this)
		(let (v ...vs) vs)
		(run v)
		(if-run (exist? vs)
			{
				(forEach vs run)
			}
			{
				(log '结束循环)
			}
		)
	}
)

(forEach [a bb d s 2 5f sd "faewf"]
	{
		(let (x) args)
		(log x 87)
	}
)
<<
a	87	
bb	87	
d	87	
s	87	
2	87	
5f	87	
sd	87	
faewf	87	
结束循环	
null

```
更多示例参照[C++/S/test/t.lisp](https://gitee.com/wy2010344/S-Lisp/blob/master/C++/S/test/t.lisp)

## 列表，字符串，注释

为了方便DSL，[]内的id类型转成字符类型；阻止求值【'xxx】类型转为id，在作用域上寻找定义。而非[]内阻止求值【'xxx】则识别为字符串，避免不必要的空格。如果构建列表的大多是id类型，建议使用【(list a b c)】这样的函数。而含有空格与换行的字符串一般仍用双引号包含。

注释用
```
`我是注释`
```
注释与双引号包含的字符串都支持换行。内部也用反斜线转义。

## 函数

函数只有一个参数，args，类似js函数内的arguments，但它是默认的数据结构列表（内部实现为单链表）。

## let表达式

{}内是表达式列表，顺序执行。其中如果表达式以let开始，如(let a 9)，则是在作用域上定义。此处的let将不被识别为函数，是属于函数的特殊标识。（即函数内有普通表达式和let表达式两种情况，普通表达式不能嵌套let表达式）

let有这样几种用法:

(let k1 v1 k2 v2 ....)

其中k如果是(a b c)形式，则类似于python的元表匹配。且如果最后一个是...c，则c为剩下的列表。如果列表比参数短，则后续参数都为null。

如果k是k*，则类似于js中的字典展开，即v必须是列表，而且是以[k1 v1 k2 v2...]这样的列表，暂时称之为kvs-map。则在作用域上增加定义k.k1，k.k2。因为kvs-map中的key可能是含有空格的字符串，则其无法以.访问(类似于JS)，因此附送了一个函数k，用(k v1)来访问其值。
> 而且由于kvs-map中k可能重复，实现时要先reverse再声明（可参考代码实现），如果在乎性能，这种不必要的展开可以不使用，原本只是想模仿js中的字典，目前看来似乎并不如js方便，也许是没有js一样成熟方便的生态环境。

id只要无空格都行，但放开容易约束难，为了减少冲突，暂时不允许id中有.与*，使字典的展开访问更像js。但这可能破坏了使用者的统一风格，所以推荐自己克隆与重新实现，~~同时因为核心语法简单，我也没有心思与精力来维护一个统一的版本。~~ 几种模式没有交叉(即列表匹配中尚无内嵌列表匹配、字典匹配），因为不想让作用域变得太复杂难以分析，目前也不考虑类似js的eval。
> 但有parse函数，类似于JSON.parse，但参数2为kvs-map，传送作用域，即容许部分表达式执行。因为没有循环引用，stringify(toString)可以将列表与函数转为字符串，像json一样传递。


## 作用域

不能像js一样通过闭包修改作用域上的定义，依我对副作用的理解，即默认是const的。但如果新声明，则在后续作用域中隐藏之前的定义。

作用域也是kvs-map，避免了传统Lisp的动态作用域。但回调函数如何知道自己？有一个内置参数this(可参见实现)。因此函数内部有两个特殊的内置参数args、this。let表达式。和其它lisp一样返回最后一个表达式的计算结果。

为什么是kvs-map而不是传统lisp的[[k1 v1][k2 v2]....]，在书写上减少了括号，在执行时减少了转换计算。

因为不喜欢宏处理时的卫生问题，而且函数与列表声明已经足够简单，不打算支持宏。

## 模块管理

内置load函数，同步加载相对路径的文件，返回该文件执行的结果。同一线程中，该文件只执行一次，结果缓存着。 在模块之外最好支持全局定义的配置文件(全局函数)。

## 内置库

if语句，内置的if相当于C系语言的三元操作符a?b:c。可自定义if-run实现C系中的if。

循环用递归。

数字目前只支持整数，我考虑因为只能动态地报错，其实没必要过早区分数字和字符串，而且浮点数计算不精确，而通常使用浮点数是金额只保留两位小数。

未来打算支持接收列表作参数，列表内甚至可以是中缀表达式，如
```lisp
(cal-money 
    [  1 + 9.8 - 7 + 
        [89 * 7 - 8] 
    ]
)
```
这种形式

列表的内部实现是链表，但像【lisp的根源】一样，只能添加head，从而保持Rest返回是仍是旧链表，不破坏，线程安全。而作用域也是链表，则函数内部声明函数的时候，内部函数的作用域继承定义之前的作用域，而对定义之后的作用域了无所知，甚至自己被挂载的id。因为函数要访问自己，内置了定义this，代表函数自身。

一种语言的成功，也许在于库，但实现丰富的库不是一个人一朝一夕的事，而且不同的人有不同的使用方法与命名。S-Lisp出发点是对js的简化，但如果像json一样灵活似乎比统一的约束更好。
> 某大师说Java的xml配置文件复杂到一定程度就是一个bug奇出效率低下的lisp，感同身受，而且每种框架的配置都有自己的语法，S-Lisp也希望充当配置文件，使配置文件一开始就运行在正确的道路上。XML对人识别可能友好，但XML\SQL因为图灵不完备也造成了众多的漏洞。同时友好也许只是习惯。与其说是作为配置语言，倒不如说作为程序主体，用面向对象的宿主语言对扩充组件。面向对象语言实现S-Lisp是天造之合，但是实现日常业务的流程式需求很不吻合，至今不是很明白各面向对象语言的初始化先后，但函数式语言是明确的。面向对象语言作为图灵完备的一种DSL有自己的长处，比如作为S-Lisp的宿主语言用于提升性能。

## 免gc，引用计数

C++的S-Lisp使用引用计数，目前保持完整的无副作用（我理解的无副作用），是完全可以免gc的，这是S-Lisp的又一大亮点。引用计数最忌讳循环引用，但S-Lisp从语法层面不涉及循环引用，不像其它语言以内存为实体，S-Lisp始终在计算，因为人需要是计算结果的信息而不是内存实体，通常是建立新的而不是修改旧的（发生循环引用，同时对别处的引用造成未知的改变，即像ES6之前无const），同时也是S-Lisp可能不如其它语言的强大。但是它不仅避开了gc的各种难题，作为一种图灵完备语言仍然拥有巨大威力，它应该能填充目前编程语言的一处空白。（使用都不需要关心retain\release，是语言内部实现的）

但gui编程需要副作用，内置cache函数
```
(let x (cache 9))
==>null
(x)
==>9
(x 8)
==>null
(x)
==>8
```
cache函数的返回函数极可能造成循环引用而使引用计数失败，内存泄漏，要小心使用，一般后端编程都是直线输入->计算->输出，不需要用到这个函数（也是S-Lisp引用计数成立的原因），也许未来在启动的时候需要 --with-cache这个参数。

## 其它

Java上的S-Lisp由于不用考虑内存回收，实现上要简单了很多。如果要获得Java平台强大的库支持，自己的做法是用Java上的js来做桥接和库。

JS上的S-Lisp要使用load同步加载，似乎得对源代码打包，再从这个打包文件里读取字符串来load。

S-Lisp的出发点是简单而不是效率，能正常运行起来，语法对人友好，甚至没考虑过尾递归优化。S-Lisp的核心是这套语法，每个人有自己的实现。
> 也许改变语法能使性能更高，但实际使用为了顺手，或满足需求，这点优化的性能可能入不敷出。

## 缺点

和JavaScript相比，没有字典类型。

kvs是列表，在声明时无法区分。

虽然可以用函数返回，但不像js中直接{k1:v1,k2:v2}声明的直接明确。

字典又很常用。

但引入字典也会引入很多问题，字典是列表的派生，增加了复杂度。

如果要直接声明字典，可能得引入另一个括号。

曾经甚至想let表达式用特殊的括号，但目前又有什么括号方便好用呢？

注释的``，单引号、双引号、大中小括号，这些都是目前在键盘上最方便找到的括号。代码在文档中，是1维2维的，用最简单的表达实现能做到的最大可能性。

XML是对人最友好，排版紧凑规律，但并不如lisp完备。
增加列表的派生类型字典，基础库里需要增加很多函数，s-lisp只是一种脚本语言，不能预防所有调用，除非以s-lisp原则(禁止循环引用)去做一门强类型的DSL语言。

## 展望

我目前考虑，S-Lisp应该可以替代含糊不清的shell脚本，简单的web-server在后端对数据库与文件系统操作，但欠缺太多实现。我个人精力有限，某些方面的知识太缺乏，甚至实现源码本身上可能都有不少问题，需要社区的帮助。我相信S-Lisp也能帮到大家许多，无论是工作还是兴趣，所以开源出来。源码与思路都很简单，大家都可以根据自己的需要实现自己的版本。到时候还希望大家不吝惜开源出来，互相学习。

（目前最需要IDE支持，语法高亮与简单错误排查。还有调试功能，像js在chrome里一样。C++版的S-Lisp在异常情况下的内存回收，顺便一说try...catch...希望像red里一样 
```lisp
(let ex 
	(try 
		{....}
	)
)
...`finally`
(if-run (exist? ex)
	{
		`处理异常`
	}
	{
		`default`
	}
)
```
try的难点在于内存回收。
）。

最后，它只是一种脚本语言，依赖于宿主语言扩展与提高性能，原打算为对自己使用js习惯的简化(逗号、函数、字典没有默认顺序)，请不要对它期待过高。

---

#### 软件架构
软件架构说明


#### 安装教程

1. xxxx
2. xxxx
3. xxxx

#### 使用说明

1. xxxx
2. xxxx
3. xxxx

#### 参与贡献

1. Fork 本项目
2. 新建 Feat_xxx 分支
3. 提交代码
4. 新建 Pull Request


#### 码云特技

1. 使用 Readme\_XXX.md 来支持不同的语言，例如 Readme\_en.md, Readme\_zh.md
2. 码云官方博客 [blog.gitee.com](https://blog.gitee.com)
3. 你可以 [https://gitee.com/explore](https://gitee.com/explore) 这个地址来了解码云上的优秀开源项目
4. [GVP](https://gitee.com/gvp) 全称是码云最有价值开源项目，是码云综合评定出的优秀开源项目
5. 码云官方提供的使用手册 [http://git.mydoc.io/](http://git.mydoc.io/)
6. 码云封面人物是一档用来展示码云会员风采的栏目 [https://gitee.com/gitee-stars/](https://gitee.com/gitee-stars/)