del MrbParser.output
del MrbParser.trace

..\..\..\SCPP\CSYacc\bin\Debug\CSYacc -c -t -v mrb_parse.jay ..\..\..\SCPP\CSYacc\skeleton.cs > MrbParser.cs
rem ..\..\..\SCPP\CSYacc\bin\Debug\CSYacc -t -v mrb_parse.jay ..\..\..\SCPP\CSYacc\skeleton.cs > MrbParser.cs
ren y.output MrbParser.output
ren y.trace MrbParser.trace

