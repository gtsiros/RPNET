# REALLY QUICK DEMO

Open the solution in visual studio and run it.  
Follow the instruction in the lower textbox.  
This is just a demo of the syntax. 
For more details try the wiki


If the program complains, you might need to adjust the location of gacutil.exe in RpNetLib.vb




# RPNET
RPN system based around .net
# what
A programming language / execution library / system, that uses rpl-like syntax and .net as its "backend".
It's written in ("visual") basic and .net 4.7.1 but it can probably run on 3.5 . Probably even lower.
# why
Write a program while executing it, change the program (while executing it), don't need a development environment to write a program. Incrementaly improve the language/system
# who
I'm george, a physics msc who is bad at physics. Bad at coding, too, but that's obvious.
# how

The basic idea is that you have a stack of stuff and any command/statement executed, operates on that stuff. if a command takes one argument, it will probably be whatever happens to exist on the top of the stack. that's the *data* stack. it is somewhat unconventional to program this way even though most major programming languages do something like this internally anyway. read about "reverse polish notation" and maybe "reverse polish lisp" or "rpl". The rplman.doc document on the 4th "goodies disk" on hpcalc.org is a good start. 

# comments

**\`** (backquote) until the end of the line

# string literal

**$ "*characters*"**

**$ *literal characters until first space, no escapes***

escapes when using doublequotes:

\\\\ -> backslash

\n -> newline

\r -> line feed

\\" -> doublequotes

\t -> tab

# string literal examples

$ blah

$ blah.blah

$ "bleh blah"

$ bleh\tblah *results with a string with the slash and t characters in it*

$ "bleh\tblah" *results in a tab character*

$ "\\"" *results in a string containing a single doublequote character

# integer literal

**# *an integer***

# integer literal examples

\# 1

\# 2222222222

# float literals

**% *a single precision float***

**%% *a double precision float***

# float literal examples

% 1.2

%% 1.2

# identifier literal

**id *any string without spaces***

# identifier literal examples

id foo

id ¯\\(°_o)/¯

id %

id """""

id id

id '

# list

**{ *objects* }**

example:

{ # 1 % 2.3 %% 45.56 $ hi id id ¯\\(°_o)/¯ }

# secondary

**:: *objects* ;**

examples:

:: # 1 + ; this program increments whatever is given at its input

$ system.windows.forms.form totype # 1 ::n ' :: new dup {} $ Show @ dup rot $ Text ! ; + $ want_form def

This is not a program, but it creates a program and defines a word for it ( *want_form* ) that when called creates a new windows form with the title text given to it. You can then add buttons and stuff to the new form.

# array

**\[ *objects* \]**

# word

A named object or the name of that object. for example, **+**, **{}**, **\[\]**, **'**, **totype**, **@**, **true** are strings that represent built-in objects. Some of them do stuff (perform computation on data) and others just give you stuff (return a somewhat constant value)

*words* can be defined, redefined and undefined. don't want **+** to represent addition any longer? no prob. **$ + undef** . want **true** to represent the object **\# -1** instead (during *this* execution only, mind you)? **\# -1 $ true dup undef def** and godspeed

# what next

Mimicking AddHandler (dynamically), debugging and exception handling
