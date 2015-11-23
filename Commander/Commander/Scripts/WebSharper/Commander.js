(function()
{
 var Global=this,Runtime=this.IntelliFactory.Runtime,PrintfHelpers,Commander,WebCommander,OrderAndPolicy,Order,Speed,FireControl,Seq,List,UI,Next,Doc,Strings,T,Var,Concurrency,Remoting,AjaxRemotingProvider,Var1,Collections,FSharpMap,View;
 Runtime.Define(Global,{
  Commander:{
   WebCommander:{
    FireControl:Runtime.Class({},{
     Show:function(fire)
     {
      return fire.$==1?"return fire only":fire.$==2?"hold fire":"free fire";
     }
    }),
    Order:Runtime.Class({},{
     Show:function(order)
     {
      var _,dest;
      if(order.$==1)
       {
        _="Continue";
       }
      else
       {
        if(order.$==2)
         {
          _="Form off-road column";
         }
        else
         {
          if(order.$==3)
           {
            _="Form on-road column";
           }
          else
           {
            if(order.$==4)
             {
              _="Attack formation";
             }
            else
             {
              if(order.$==5)
               {
                _="Fire a green flare";
               }
              else
               {
                if(order.$==6)
                 {
                  _="Fire a red flare";
                 }
                else
                 {
                  if(order.$==7)
                   {
                    dest=order.$0;
                    _=dest;
                   }
                  else
                   {
                    _="Stop";
                   }
                 }
               }
             }
           }
         }
       }
      return _;
     }
    }),
    OrderAndPolicy:Runtime.Class({
     ToServerInput:function(platoon,compressDestination)
     {
      var _,_1,dest,dest1,policy,matchValue,prefix1,matchValue1,prefix2,_2;
      if(this.$0.$==1)
       {
        _="Order_"+PrintfHelpers.toSafe(platoon)+"_Continue";
       }
      else
       {
        if(this.$0.$==2)
         {
          _="Order_"+PrintfHelpers.toSafe(platoon)+"_Column";
         }
        else
         {
          if(this.$0.$==3)
           {
            _="Order_"+PrintfHelpers.toSafe(platoon)+"_OnRoad";
           }
          else
           {
            if(this.$0.$==4)
             {
              _="Order_"+PrintfHelpers.toSafe(platoon)+"_Attack";
             }
            else
             {
              if(this.$0.$==5)
               {
                _="Order_"+PrintfHelpers.toSafe(platoon)+"_GreenFlare";
               }
              else
               {
                if(this.$0.$==6)
                 {
                  _="Order_"+PrintfHelpers.toSafe(platoon)+"_RedFlare";
                 }
                else
                 {
                  if(this.$0.$==7)
                   {
                    if(this.$1.$==0)
                     {
                      dest=this.$0.$0;
                      _1=Runtime.New(OrderAndPolicy,{
                       $:0,
                       $0:Runtime.New(Order,{
                        $:7,
                        $0:dest
                       }),
                       $1:{
                        $:1,
                        $0:{
                         Speed:Runtime.New(Speed,{
                          $:1
                         }),
                         FireControl:Runtime.New(FireControl,{
                          $:1
                         })
                        }
                       }
                      }).ToServerInput(platoon,compressDestination);
                     }
                    else
                     {
                      dest1=this.$0.$0;
                      policy=this.$1.$0;
                      matchValue=policy.FireControl;
                      prefix1=matchValue.$==1?"R":matchValue.$==2?"H":"F";
                      matchValue1=policy.Speed;
                      prefix2=matchValue1.$==1?"N":matchValue1.$==2?"F":"S";
                      _2=compressDestination(dest1);
                      _1=PrintfHelpers.toSafe(prefix1)+PrintfHelpers.toSafe(prefix2)+"_"+PrintfHelpers.toSafe(platoon)+"_"+PrintfHelpers.toSafe(_2);
                     }
                    _=_1;
                   }
                  else
                   {
                    _="Order_"+PrintfHelpers.toSafe(platoon)+"_Stop";
                   }
                 }
               }
             }
           }
         }
       }
      return _;
     }
    }),
    ShowPlayers:function(players)
    {
     var arg20;
     arg20=Seq.toList(Seq.delay(function()
     {
      var arg201,arg202,arg203;
      arg202=List.ofArray([Doc.TextNode("Name")]);
      arg203=List.ofArray([Doc.TextNode("Ping")]);
      arg201=List.ofArray([Doc.Element("td",[],arg202),Doc.Element("td",[],arg203)]);
      return Seq.append([Doc.Element("tr",[],arg201)],Seq.delay(function()
      {
       return Seq.map(function(player)
       {
        var arg204,arg205,arg206;
        arg205=List.ofArray([Doc.TextNode(PrintfHelpers.toSafe(player.Name))]);
        arg206=List.ofArray([Doc.TextNode(Strings.PadLeft(Global.String(player.Ping),3))]);
        arg204=List.ofArray([Doc.Element("td",[],arg205),Doc.Element("td",[],arg206)]);
        return Doc.Element("tr",[],arg204);
       },players);
      }));
     }));
     return Doc.Element("table",[],arg20);
    },
    ShowResult:function(result)
    {
     var mapping,list,ch;
     mapping=function(tupledArg)
     {
      var k,v;
      k=tupledArg[0];
      v=tupledArg[1];
      return"Key: "+PrintfHelpers.toSafe(k)+" Value: "+PrintfHelpers.toSafe(v);
     };
     list=List.map(mapping,result);
     ch=List.map(function(t)
     {
      return Doc.TextNode(t);
     },list);
     return Doc.Element("div",[],ch);
    },
    Speed:Runtime.Class({},{
     Show:function(speed)
     {
      return speed.$==1?"normal speed":speed.$==2?"maximum speed":"slow speed";
     }
    }),
    TakeOrders:function(waypoints,platoons)
    {
     var arg00,rvState,commandSection,arg101,_arg00_;
     arg00={
      User:{
       $:0
      },
      Platoons:Runtime.New(T,{
       $:0
      }),
      LoginMessage:{
       $:0
      }
     };
     rvState=Var.Create(arg00);
     commandSection=function(state)
     {
      var matchValue,login,_,user,arg001,password,arg20,arg201,attrs,arg202,matchValue1,_2,msg,matchValue2,platoonSelection,_3,user1,predicate,available,_4,first,chosen,arg203,arg30,mapping,x1,folder,state1,wpRank,compressDestination,projection,mapping1,list,moveTo,orderList,speedList,fireList,mkRow,arg209,orderAssignment,matchValue3,logout,_5,arg20a,arg20b;
      matchValue=state.User;
      if(matchValue.$==1)
       {
        user=matchValue.$0;
        arg001="Command panel for "+PrintfHelpers.toSafe(user);
        _=Doc.TextNode(arg001);
       }
      else
       {
        password=Var.Create("AAA1234");
        attrs=Runtime.New(T,{
         $:0
        });
        arg202=function()
        {
         return AjaxRemotingProvider.Send("Commander:3",[]);
        };
        arg201=List.ofArray([Doc.TextNode("Your PIN: "),Doc.Input(Runtime.New(T,{
         $:0
        }),password),Doc.Button("Log in",attrs,function()
        {
         var arg002;
         arg002=Concurrency.Delay(function()
         {
          var x;
          x=AjaxRemotingProvider.Async("Commander:2",[Var1.Get(password)]);
          return Concurrency.Bind(x,function(_arg1)
          {
           var status,_1,username,arg10;
           if(_arg1.$==0)
            {
             _1={
              $:1,
              $0:"Incorrect pin code"
             };
            }
           else
            {
             username=_arg1.$0;
             _1={
              $:1,
              $0:"Welcome "+PrintfHelpers.toSafe(username)
             };
            }
           status=_1;
           arg10={
            User:_arg1,
            Platoons:state.Platoons,
            LoginMessage:status
           };
           Var1.Set(rvState,arg10);
           return Concurrency.Return(null);
          });
         });
         return Concurrency.Start(arg002,{
          $:0
         });
        }),Doc.Button("Request PIN",Runtime.New(T,{
         $:0
        }),arg202)]);
        matchValue1=state.LoginMessage;
        if(matchValue1.$==1)
         {
          msg=matchValue1.$0;
          _2=msg;
         }
        else
         {
          _2="";
         }
        arg20=List.ofArray([Doc.Element("div",[],arg201),Doc.TextNode(_2)]);
        _=Doc.Element("div",[],arg20);
       }
      login=_;
      matchValue2=state.User;
      if(matchValue2.$==1)
       {
        user1=matchValue2.$0;
        predicate=function(platoon)
        {
         var value;
         value=List.contains(platoon,state.Platoons);
         return!value;
        };
        available=List.filter(predicate,platoons);
        if(available.$==1)
         {
          first=available.$0;
          chosen=Var.Create(first);
          arg30=function(platoon)
          {
           var Platoons,state2;
           Platoons=Runtime.New(T,{
            $:1,
            $0:platoon,
            $1:state.Platoons
           });
           state2={
            User:state.User,
            Platoons:Platoons,
            LoginMessage:state.LoginMessage
           };
           return Var1.Set(rvState,state2);
          };
          arg203=List.ofArray([Doc.Select(Runtime.New(T,{
           $:0
          }),function(x)
          {
           return x;
          },available,chosen),Doc.ButtonView("Add",Runtime.New(T,{
           $:0
          }),chosen.get_View(),arg30)]);
          _4=Doc.Element("div",[],arg203);
         }
        else
         {
          _4=Doc.TextNode("All platoons assigned");
         }
        _3=_4;
       }
      else
       {
        _3=Doc.get_Empty();
       }
      platoonSelection=_3;
      mapping=function(i)
      {
       return function(wp)
       {
        return[wp,i];
       };
      };
      x1=List.mapi(mapping,waypoints);
      folder=function(m)
      {
       return function(tupledArg)
       {
        var wp,i;
        wp=tupledArg[0];
        i=tupledArg[1];
        return m.Add(wp,i);
       };
      };
      state1=FSharpMap.New1([]);
      wpRank=Seq.fold(folder,state1,x1);
      compressDestination=function(wp)
      {
       return"WP"+Global.String(wpRank.get_Item(wp));
      };
      projection=function(_arg2)
      {
       return _arg2==="North"?[0,{
        $:0
       }]:_arg2==="North-East"?[1,{
        $:0
       }]:_arg2==="East"?[2,{
        $:0
       }]:_arg2==="South-East"?[3,{
        $:0
       }]:_arg2==="South"?[4,{
        $:0
       }]:_arg2==="South-West"?[5,{
        $:0
       }]:_arg2==="West"?[6,{
        $:0
       }]:_arg2==="North-West"?[7,{
        $:0
       }]:[8,{
        $:1,
        $0:_arg2
       }];
      };
      mapping1=function(name)
      {
       return Runtime.New(Order,{
        $:7,
        $0:name
       });
      };
      list=List.sortBy(projection,waypoints);
      moveTo=List.map(mapping1,list);
      orderList=List.ofArray([Runtime.New(Order,{
       $:3
      }),Runtime.New(Order,{
       $:2
      }),Runtime.New(Order,{
       $:4
      })]);
      speedList=List.ofArray([Runtime.New(Speed,{
       $:0
      }),Runtime.New(Speed,{
       $:2
      })]);
      fireList=List.ofArray([Runtime.New(FireControl,{
       $:0
      }),Runtime.New(FireControl,{
       $:1
      })]);
      mkRow=function(platoon)
      {
       var chosenOrder,chosenDestination,chosenSpeed,chosenFire,arg204,arg205,arg206,arg207,arg208;
       chosenOrder=Var.Create(List.head(orderList));
       chosenDestination=Var.Create(List.head(moveTo));
       chosenSpeed=Var.Create(List.head(speedList));
       chosenFire=Var.Create(List.head(fireList));
       arg205=function()
       {
        var orderAndPolicy;
        orderAndPolicy=Runtime.New(OrderAndPolicy,{
         $:0,
         $0:Var1.Get(chosenOrder),
         $1:{
          $:0
         }
        });
        return AjaxRemotingProvider.Send("Commander:0",[orderAndPolicy.ToServerInput(platoon,compressDestination)]);
       };
       arg206=function()
       {
        var orderAndPolicy;
        orderAndPolicy=Runtime.New(OrderAndPolicy,{
         $:0,
         $0:Var1.Get(chosenDestination),
         $1:{
          $:1,
          $0:{
           Speed:Var1.Get(chosenSpeed),
           FireControl:Var1.Get(chosenFire)
          }
         }
        });
        return AjaxRemotingProvider.Send("Commander:0",[orderAndPolicy.ToServerInput(platoon,compressDestination)]);
       };
       arg207=function()
       {
        return AjaxRemotingProvider.Send("Commander:0",[Runtime.New(OrderAndPolicy,{
         $:0,
         $0:Runtime.New(Order,{
          $:0
         }),
         $1:{
          $:0
         }
        }).ToServerInput(platoon,compressDestination)]);
       };
       arg208=function()
       {
        return AjaxRemotingProvider.Send("Commander:0",[Runtime.New(OrderAndPolicy,{
         $:0,
         $0:Runtime.New(Order,{
          $:1
         }),
         $1:{
          $:0
         }
        }).ToServerInput(platoon,compressDestination)]);
       };
       arg204=List.ofArray([Doc.TextNode(platoon),Doc.Select(Runtime.New(T,{
        $:0
       }),function(arg002)
       {
        return Order.Show(arg002);
       },orderList,chosenOrder),Doc.Button("Order",Runtime.New(T,{
        $:0
       }),arg205),Doc.TextNode(" - "),Doc.TextNode("Move towards..."),Doc.Select(Runtime.New(T,{
        $:0
       }),function(arg002)
       {
        return Order.Show(arg002);
       },moveTo,chosenDestination),Doc.TextNode(" at "),Doc.Select(Runtime.New(T,{
        $:0
       }),function(arg002)
       {
        return Speed.Show(arg002);
       },speedList,chosenSpeed),Doc.TextNode(", "),Doc.Select(Runtime.New(T,{
        $:0
       }),function(arg002)
       {
        return FireControl.Show(arg002);
       },fireList,chosenFire),Doc.Button("Move",Runtime.New(T,{
        $:0
       }),arg206),Doc.TextNode(" - "),Doc.Button("Stop",Runtime.New(T,{
        $:0
       }),arg207),Doc.TextNode(" "),Doc.Button("Continue",Runtime.New(T,{
        $:0
       }),arg208)]);
       return Doc.Element("div",[],arg204);
      };
      arg209=Seq.toList(Seq.delay(function()
      {
       return Seq.map(function(platoon)
       {
        return mkRow(platoon);
       },state.Platoons);
      }));
      orderAssignment=Doc.Element("div",[],arg209);
      matchValue3=state.User;
      if(matchValue3.$==1)
       {
        matchValue3.$0;
        arg20a=function()
        {
         var arg10;
         arg10={
          User:{
           $:0
          },
          Platoons:Runtime.New(T,{
           $:0
          }),
          LoginMessage:state.LoginMessage
         };
         return Var1.Set(rvState,arg10);
        };
        _5=Doc.Button("Log out",Runtime.New(T,{
         $:0
        }),arg20a);
       }
      else
       {
        _5=Doc.get_Empty();
       }
      logout=_5;
      arg20b=List.ofArray([login,platoonSelection,orderAssignment,logout]);
      return Doc.Element("div",[],arg20b);
     };
     arg101=rvState.get_View();
     _arg00_=View.Map(commandSection,arg101);
     return Doc.EmbedView(_arg00_);
    }
   }
  }
 });
 Runtime.OnInit(function()
 {
  PrintfHelpers=Runtime.Safe(Global.WebSharper.PrintfHelpers);
  Commander=Runtime.Safe(Global.Commander);
  WebCommander=Runtime.Safe(Commander.WebCommander);
  OrderAndPolicy=Runtime.Safe(WebCommander.OrderAndPolicy);
  Order=Runtime.Safe(WebCommander.Order);
  Speed=Runtime.Safe(WebCommander.Speed);
  FireControl=Runtime.Safe(WebCommander.FireControl);
  Seq=Runtime.Safe(Global.WebSharper.Seq);
  List=Runtime.Safe(Global.WebSharper.List);
  UI=Runtime.Safe(Global.WebSharper.UI);
  Next=Runtime.Safe(UI.Next);
  Doc=Runtime.Safe(Next.Doc);
  Strings=Runtime.Safe(Global.WebSharper.Strings);
  T=Runtime.Safe(List.T);
  Var=Runtime.Safe(Next.Var);
  Concurrency=Runtime.Safe(Global.WebSharper.Concurrency);
  Remoting=Runtime.Safe(Global.WebSharper.Remoting);
  AjaxRemotingProvider=Runtime.Safe(Remoting.AjaxRemotingProvider);
  Var1=Runtime.Safe(Next.Var1);
  Collections=Runtime.Safe(Global.WebSharper.Collections);
  FSharpMap=Runtime.Safe(Collections.FSharpMap);
  return View=Runtime.Safe(Next.View);
 });
 Runtime.OnLoad(function()
 {
  return;
 });
}());
