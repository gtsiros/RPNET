

# Quick Demo

Load it, run it, a console should open.
write (spaces between **\#** and the numbers are *required*):

**\# 1 \# 2 +**

congrats you performed addition

**\# 1 \# 10 do ?i loop**

now you have 11 items on the stack

**\# 1 \# 11 do drop loop**

now they're gone

# RPNET
If you've ever programmed a Hewlett Packard calculator in sysRPL, this is exactly that.

RPL is a [concatenative](https://en.wikipedia.org/wiki/Concatenative_programming_language), [reflective](https://en.wikipedia.org/wiki/Reflection_(computer_programming)), [stack-oriented](https://en.wikipedia.org/wiki/Stack-oriented_programming_language), [threaded-interpreted](https://en.wikipedia.org/wiki/Threaded_code) [metaprogramming](https://en.wikipedia.org/wiki/Metaprogramming) language and system all in one.

A somewhat thorough explanation of how the system works is in the document [rplman.doc](https://www.hpcalc.org/details/1743). That document is a guide for RPNET.
# what

It's written in VB ( or C#, i switch from one to the other occassionaly) for .net 4.7.1 but it should run on 3.5 . Probably even lower.

# why
Write a program while executing it, change the program (while executing it), don't need a development environment to write a program. Incrementaly improve the language/system
# who
I'm george, a physicist and coder.
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

# arrays

you can have (typed) arrays of literals by enclosing them in brackets

id \[ a b c d \]  
\# \[ 1 2 3 \]  
\%% \[ 1.2 2.3 3.4 \]  
etc

# lists

**{ *objects separated by whatever it is is they are separated* }**

note: **{** and **}** are not PUNCTUATION they are COMMANDS much like **::** and **;**.  
In fact, **}** is functionally equivalent to **;**

example:

{ # 1 % 2.3 %% 45.56 $ hi id id ¯\\(°_o)/¯ }

this becomes a list with the following objects:  
an integer with the value **1**  
a single precision float with the value **2.3**  
a double precision float with the value **45.56**  
a character string with the characters **hi** in it  
an identifier with the name **id**  
a command with the name **¯\\°_O/¯** so if you've previously defined such a word, it will be called.

# secondary

**:: *objects* ;**

examples:

:: # 1 + ; this program increments whatever is given at its input


:: ' :: <system.windows.forms.form> new drop dup # 3 pick $ Text ! dup $ Show @ ; $ want_form define ; 

This is not a program, but it creates a program and defines a word for it ( *want_form* ) that when called creates a new windows form with the title text given to it. You can then add buttons and stuff to the new form.

# word

A named object or the name of that object. for example, **+**, **{}**, **\[\]**, **'**, **;**, **true** are sequences of characters that represent built-in objects. Some of them do stuff (perform computation on data) and others just give you stuff (return a somewhat constant value)

*words* can be defined, redefined and undefined. don't want **+** to represent addition any longer? no prob. **$ + undef** (not yet implemented, but trivial) . want **true** to represent the object **\# -1** instead (during *this* instance of the class, mind you)? **\# -1 $ true dup undef define** (and good luck)

# what next

Too much.
