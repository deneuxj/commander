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
     Description:function(platoon)
     {
      var _,_1,dest,dest1,policy,matchValue,prefix1,matchValue1,prefix2;
      if(this.$0.$==1)
       {
        _=PrintfHelpers.toSafe(platoon)+": Continue";
       }
      else
       {
        if(this.$0.$==2)
         {
          _=PrintfHelpers.toSafe(platoon)+": Column";
         }
        else
         {
          if(this.$0.$==3)
           {
            _=PrintfHelpers.toSafe(platoon)+": On road";
           }
          else
           {
            if(this.$0.$==4)
             {
              _=PrintfHelpers.toSafe(platoon)+": Attack formation";
             }
            else
             {
              if(this.$0.$==5)
               {
                _=PrintfHelpers.toSafe(platoon)+": Fire green flare";
               }
              else
               {
                if(this.$0.$==6)
                 {
                  _=PrintfHelpers.toSafe(platoon)+": Fire red flare";
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
                      }).Description(platoon);
                     }
                    else
                     {
                      dest1=this.$0.$0;
                      policy=this.$1.$0;
                      matchValue=policy.FireControl;
                      prefix1=matchValue.$==1?"return fire":matchValue.$==2?"hold fire":"free fire";
                      matchValue1=policy.Speed;
                      prefix2=matchValue1.$==1?"normal":matchValue1.$==2?"fast":"slow";
                      _1=PrintfHelpers.toSafe(platoon)+": Move to "+PrintfHelpers.toSafe(dest1)+" at "+PrintfHelpers.toSafe(prefix2)+" speed, "+PrintfHelpers.toSafe(prefix1);
                     }
                    _=_1;
                   }
                  else
                   {
                    _=PrintfHelpers.toSafe(platoon)+": Stop";
                   }
                 }
               }
             }
           }
         }
       }
      return _;
     },
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
    TakeOrders:function(waypoints,axisPlatoons,alliedPlatoons)
    {
     var arg00,rvState,commandSection,arg102,_arg00_;
     arg00={
      User:{
       $:0
      },
      Coalition:{
       $:0
      },
      Available:Runtime.New(T,{
       $:0
      }),
      Platoons:Runtime.New(T,{
       $:0
      }),
      LoginMessage:{
       $:0
      },
      GrabMessage:{
       $:0
      }
     };
     rvState=Var.Create(arg00);
     commandSection=function(state)
     {
      var matchValue,login,_,user,arg001,coalitionPwd,userPwd,arg20,arg201,attrs,arg202,matchValue1,_3,msg,matchValue2,platoonSelection,_4,_5,coalition2,user3,matchValue3,_6,first,chosen,arg203,options,attrs1,view,mapping,x2,folder,state1,wpRank,compressDestination,projection,mapping1,list,moveTo,orderList,speedList,fireList,matchValue4,orderAssignment,_7,user4,tryGiveOrder,mkRow,arg209,matchValue6,logout,_8,user5,attrs3,arg20a;
      matchValue=state.User;
      if(matchValue.$==1)
       {
        user=matchValue.$0.$0;
        arg001="Command panel for "+PrintfHelpers.toSafe(user);
        _=Doc.TextNode(arg001);
       }
      else
       {
        coalitionPwd=Var.Create("AAA");
        userPwd=Var.Create("1234");
        attrs=Runtime.New(T,{
         $:0
        });
        arg202=function()
        {
         return AjaxRemotingProvider.Send("Commander:3",[true]);
        };
        arg201=List.ofArray([Doc.TextNode("Your coalition code (3 letters) and your pin (4 digits):"),Doc.Input(Runtime.New(T,{
         $:0
        }),coalitionPwd),Doc.Input(Runtime.New(T,{
         $:0
        }),userPwd),Doc.TextNode(" "),Doc.Button("Log in",attrs,function()
        {
         var arg002;
         arg002=Concurrency.Delay(function()
         {
          var x;
          x=AjaxRemotingProvider.Async("Commander:2",[Var1.Get(userPwd),Var1.Get(coalitionPwd).toUpperCase()]);
          return Concurrency.Bind(x,function(_arg1)
          {
           var patternInput,_1,username,user1,coalition,_2,user2,status,coalition1,platoons;
           if(_arg1.$==0)
            {
             _1=[{
              $:0
             },{
              $:0
             },{
              $:1,
              $0:"Incorrect pin code"
             }];
            }
           else
            {
             username=_arg1.$0[0].$0;
             user1=_arg1.$0[0];
             coalition=_arg1.$0[1];
             _2=coalition.AsString();
             _1=[{
              $:1,
              $0:user1
             },{
              $:1,
              $0:coalition
             },{
              $:1,
              $0:"Welcome "+PrintfHelpers.toSafe(username)+" in coalition "+PrintfHelpers.toSafe(_2)
             }];
            }
           patternInput=_1;
           user2=patternInput[0];
           status=patternInput[2];
           coalition1=patternInput[1];
           platoons=coalition1.$==0?Runtime.New(T,{
            $:0
           }):coalition1.$0.$==0?axisPlatoons:alliedPlatoons;
           return Concurrency.Bind(AjaxRemotingProvider.Async("Commander:4",[platoons]),function(_arg2)
           {
            var arg10;
            arg10={
             User:user2,
             Coalition:coalition1,
             Available:_arg2,
             Platoons:state.Platoons,
             LoginMessage:status,
             GrabMessage:state.GrabMessage
            };
            Var1.Set(rvState,arg10);
            return Concurrency.Return(null);
           });
          });
         });
         return Concurrency.Start(arg002,{
          $:0
         });
        }),Doc.TextNode(" "),Doc.Button("Request PIN",Runtime.New(T,{
         $:0
        }),arg202)]);
        matchValue1=state.LoginMessage;
        if(matchValue1.$==1)
         {
          msg=matchValue1.$0;
          _3=msg;
         }
        else
         {
          _3="";
         }
        arg20=List.ofArray([Doc.Element("div",[],arg201),Doc.TextNode(_3)]);
        _=Doc.Element("div",[],arg20);
       }
      login=_;
      matchValue2=[state.User,state.Coalition];
      if(matchValue2[1].$==1)
       {
        if(matchValue2[0].$==1)
         {
          coalition2=matchValue2[1].$0;
          user3=matchValue2[0].$0;
          matchValue3=state.Available;
          if(matchValue3.$==1)
           {
            first=matchValue3.$0;
            chosen=Var.Create(first);
            options=state.Available;
            attrs1=Runtime.New(T,{
             $:0
            });
            view=chosen.get_View();
            arg203=List.ofArray([Doc.Select(Runtime.New(T,{
             $:0
            }),function(x)
            {
             return x.AsString();
            },options,chosen),Doc.ButtonView("Add",attrs1,view,function(platoon)
            {
             var arg002;
             arg002=Concurrency.Delay(function()
             {
              var x;
              x=AjaxRemotingProvider.Async("Commander:5",[user3,platoon]);
              return Concurrency.Bind(x,function(_arg3)
              {
               var platoons,x1;
               platoons=coalition2.$==0?axisPlatoons:alliedPlatoons;
               x1=AjaxRemotingProvider.Async("Commander:4",[platoons]);
               return Concurrency.Bind(x1,function(_arg4)
               {
                var _1,Platoons,GrabMessage,arg10,GrabMessage1,arg101;
                if(_arg3)
                 {
                  Platoons=Runtime.New(T,{
                   $:1,
                   $0:platoon,
                   $1:state.Platoons
                  });
                  GrabMessage={
                   $:1,
                   $0:"Platoon grabbed: "+PrintfHelpers.toSafe(platoon.AsString())
                  };
                  arg10={
                   User:state.User,
                   Coalition:state.Coalition,
                   Available:_arg4,
                   Platoons:Platoons,
                   LoginMessage:state.LoginMessage,
                   GrabMessage:GrabMessage
                  };
                  Var1.Set(rvState,arg10);
                  _1=Concurrency.Return(null);
                 }
                else
                 {
                  GrabMessage1={
                   $:1,
                   $0:"Could not grab control of "+PrintfHelpers.toSafe(platoon.AsString())
                  };
                  arg101={
                   User:state.User,
                   Coalition:state.Coalition,
                   Available:_arg4,
                   Platoons:state.Platoons,
                   LoginMessage:state.LoginMessage,
                   GrabMessage:GrabMessage1
                  };
                  Var1.Set(rvState,arg101);
                  _1=Concurrency.Return(null);
                 }
                return _1;
               });
              });
             });
             return Concurrency.Start(arg002,{
              $:0
             });
            })]);
            _6=Doc.Element("div",[],arg203);
           }
          else
           {
            _6=Doc.TextNode("All platoons assigned");
           }
          _5=_6;
         }
        else
         {
          _5=Doc.get_Empty();
         }
        _4=_5;
       }
      else
       {
        _4=Doc.get_Empty();
       }
      platoonSelection=_4;
      mapping=function(i)
      {
       return function(wp)
       {
        return[wp,i];
       };
      };
      x2=List.mapi(mapping,waypoints);
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
      wpRank=Seq.fold(folder,state1,x2);
      compressDestination=function(wp)
      {
       return"WP"+Global.String(wpRank.get_Item(wp));
      };
      projection=function(_arg5)
      {
       return _arg5==="North"?[0,{
        $:0
       }]:_arg5==="North-East"?[1,{
        $:0
       }]:_arg5==="East"?[2,{
        $:0
       }]:_arg5==="South-East"?[3,{
        $:0
       }]:_arg5==="South"?[4,{
        $:0
       }]:_arg5==="South-West"?[5,{
        $:0
       }]:_arg5==="West"?[6,{
        $:0
       }]:_arg5==="North-West"?[7,{
        $:0
       }]:[8,{
        $:1,
        $0:_arg5
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
      matchValue4=state.User;
      if(matchValue4.$==0)
       {
        _7=Doc.get_Empty();
       }
      else
       {
        user4=matchValue4.$0;
        tryGiveOrder=function(platoon)
        {
         return function(orderAndPolicy)
         {
          return Concurrency.Delay(function()
          {
           var x;
           x=AjaxRemotingProvider.Async("Commander:7",[user4,platoon]);
           return Concurrency.Bind(x,function(_arg6)
           {
            var _1,orderString;
            if(_arg6)
             {
              orderString=orderAndPolicy.ToServerInput(platoon.AsString(),compressDestination);
              AjaxRemotingProvider.Send("Commander:0",[orderString,user4,orderAndPolicy.Description(platoon.AsString())]);
              _1=Concurrency.Delay(function()
              {
               return Concurrency.Return(null);
              });
             }
            else
             {
              _1=Concurrency.Delay(function()
              {
               return Concurrency.Bind(AjaxRemotingProvider.Async("Commander:8",[user4]),function(_arg7)
               {
                var arg10;
                arg10={
                 User:state.User,
                 Coalition:state.Coalition,
                 Available:state.Available,
                 Platoons:_arg7,
                 LoginMessage:state.LoginMessage,
                 GrabMessage:state.GrabMessage
                };
                Var1.Set(rvState,arg10);
                return Concurrency.Return(null);
               });
              });
             }
            return _1;
           });
          });
         };
        };
        mkRow=function(platoon)
        {
         var chosenOrder,chosenDestination,chosenSpeed,chosenFire,arg204,attrs2,arg205,arg206,arg207,arg208;
         chosenOrder=Var.Create(List.head(orderList));
         chosenDestination=Var.Create(List.head(moveTo));
         chosenSpeed=Var.Create(List.head(speedList));
         chosenFire=Var.Create(List.head(fireList));
         attrs2=Runtime.New(T,{
          $:0
         });
         arg205=function()
         {
          var x,arg002;
          x=Runtime.New(OrderAndPolicy,{
           $:0,
           $0:Var1.Get(chosenOrder),
           $1:{
            $:0
           }
          });
          arg002=(tryGiveOrder(platoon))(x);
          return Concurrency.Start(arg002,{
           $:0
          });
         };
         arg206=function()
         {
          var x,arg002;
          x=Runtime.New(OrderAndPolicy,{
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
          arg002=(tryGiveOrder(platoon))(x);
          return Concurrency.Start(arg002,{
           $:0
          });
         };
         arg207=function()
         {
          var x,arg002;
          x=Runtime.New(OrderAndPolicy,{
           $:0,
           $0:Runtime.New(Order,{
            $:0
           }),
           $1:{
            $:0
           }
          });
          arg002=(tryGiveOrder(platoon))(x);
          return Concurrency.Start(arg002,{
           $:0
          });
         };
         arg208=function()
         {
          var x,arg002;
          x=Runtime.New(OrderAndPolicy,{
           $:0,
           $0:Runtime.New(Order,{
            $:1
           }),
           $1:{
            $:0
           }
          });
          arg002=(tryGiveOrder(platoon))(x);
          return Concurrency.Start(arg002,{
           $:0
          });
         };
         arg204=List.ofArray([Doc.Button("X",attrs2,function()
         {
          var arg002;
          arg002=Concurrency.Delay(function()
          {
           var x;
           AjaxRemotingProvider.Send("Commander:6",[user4,platoon]);
           x=AjaxRemotingProvider.Async("Commander:8",[user4]);
           return Concurrency.Bind(x,function(_arg8)
           {
            var matchValue5,platoons;
            matchValue5=state.Coalition;
            platoons=matchValue5.$==0?Runtime.New(T,{
             $:0
            }):matchValue5.$0.$==0?axisPlatoons:alliedPlatoons;
            return Concurrency.Bind(AjaxRemotingProvider.Async("Commander:4",[platoons]),function(_arg9)
            {
             var arg10;
             arg10={
              User:state.User,
              Coalition:state.Coalition,
              Available:_arg9,
              Platoons:_arg8,
              LoginMessage:state.LoginMessage,
              GrabMessage:state.GrabMessage
             };
             Var1.Set(rvState,arg10);
             return Concurrency.Return(null);
            });
           });
          });
          return Concurrency.Start(arg002,{
           $:0
          });
         }),Doc.TextNode(" "),Doc.TextNode(platoon.AsString()),Doc.TextNode(" "),Doc.Select(Runtime.New(T,{
          $:0
         }),function(arg002)
         {
          return Order.Show(arg002);
         },orderList,chosenOrder),Doc.TextNode(" "),Doc.Button("Order",Runtime.New(T,{
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
        _7=Doc.Element("div",[],arg209);
       }
      orderAssignment=_7;
      matchValue6=state.User;
      if(matchValue6.$==1)
       {
        user5=matchValue6.$0;
        attrs3=Runtime.New(T,{
         $:0
        });
        _8=Doc.Button("Log out",attrs3,function()
        {
         var User,Platoons,Coalition,LoginMessage,arg10;
         AjaxRemotingProvider.Send("Commander:9",[user5]);
         User={
          $:0
         };
         Platoons=Runtime.New(T,{
          $:0
         });
         Coalition={
          $:0
         };
         LoginMessage={
          $:0
         };
         arg10={
          User:User,
          Coalition:Coalition,
          Available:state.Available,
          Platoons:Platoons,
          LoginMessage:LoginMessage,
          GrabMessage:state.GrabMessage
         };
         return Var1.Set(rvState,arg10);
        });
       }
      else
       {
        _8=Doc.get_Empty();
       }
      logout=_8;
      arg20a=List.ofArray([login,platoonSelection,orderAssignment,logout]);
      return Doc.Element("div",[],arg20a);
     };
     arg102=rvState.get_View();
     _arg00_=View.Map(commandSection,arg102);
     return Doc.EmbedView(_arg00_);
    }
   }
  },
  Users:{
   Coalition:Runtime.Class({
    AsString:function()
    {
     return this.$==1?"Allies":"Axis";
    }
   },{
    IdOf:function(coalition)
    {
     return coalition.$==1?1:2;
    }
   }),
   Unit:Runtime.Class({
    AsString:function()
    {
     var name;
     name=this.$0;
     return name;
    }
   }),
   User:Runtime.Class({
    AsString:function()
    {
     var name;
     name=this.$0;
     return name;
    }
   })
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
