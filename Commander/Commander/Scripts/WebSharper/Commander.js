(function()
{
 var Global=this,Runtime=this.IntelliFactory.Runtime,Commander,GameEvents,State,UI,Next,Var,Doc,List,T,Concurrency,Remoting,AjaxRemotingProvider,Var1,View,PrintfHelpers,WebCommander,OrderAndPolicy,Order,Speed,FireControl,Seq,Strings,Collections,FSharpMap,Unchecked;
 Runtime.Define(Global,{
  Commander:{
   GameEvents:{
    State:Runtime.Class({},{
     get_Default:function()
     {
      return Runtime.New(State,{
       Authenticated:false,
       Password:""
      });
     }
    }),
    showEvents:function(events)
    {
     var rvState,eventsSection,arg202;
     rvState=Var.Create(State.get_Default());
     eventsSection=function(state)
     {
      var matchValue,event,item,arg20,arg201;
      matchValue=[state,events];
      if(matchValue[0].Authenticated)
       {
        if(matchValue[1].$==0)
         {
          return Doc.get_Empty();
         }
        else
         {
          event=matchValue[1].$0;
          item=Var.Create(event);
          arg201=function()
          {
           return Concurrency.Start(Concurrency.Delay(function()
           {
            AjaxRemotingProvider.Send("Commander:12",[state.Password,(Var1.Get(item))[0]]);
            return Concurrency.Return(null);
           }),{
            $:0
           });
          };
          arg20=List.ofArray([Doc.Select(Runtime.New(T,{
           $:0
          }),function(tuple)
          {
           return tuple[1];
          },events,item),Doc.TextNode(" "),Doc.Button("Send",Runtime.New(T,{
           $:0
          }),arg201)]);
          return Doc.Element("div",[],arg20);
         }
       }
      else
       {
        return Doc.get_Empty();
       }
     };
     arg202=List.ofArray([Doc.EmbedView(View.Map(function(state)
     {
      var arg20,arg201,pwd,arg203,arg204;
      if(state.Authenticated)
       {
        arg201=function()
        {
         return Var1.Set(rvState,State.get_Default());
        };
        arg20=List.ofArray([Doc.Button("Log out",Runtime.New(T,{
         $:0
        }),arg201)]);
        return Doc.Element("div",[],arg20);
       }
      else
       {
        pwd=Var.Create(state.Password);
        arg204=function()
        {
         return Concurrency.Start(Concurrency.Delay(function()
         {
          return Concurrency.Bind(AjaxRemotingProvider.Async("Commander:11",[Var1.Get(pwd)]),function(_arg1)
          {
           Var1.Set(rvState,Runtime.New(State,{
            Authenticated:_arg1,
            Password:Var1.Get(pwd)
           }));
           return Concurrency.Return(null);
          });
         }),{
          $:0
         });
        };
        arg203=List.ofArray([Doc.TextNode("Password: "),Doc.Input(Runtime.New(T,{
         $:0
        }),pwd),Doc.TextNode(" "),Doc.Button("Send",Runtime.New(T,{
         $:0
        }),arg204)]);
        return Doc.Element("div",[],arg203);
       }
     },rvState.get_View())),Doc.EmbedView(View.Map(eventsSection,rvState.get_View()))]);
     return Doc.Element("div",[],arg202);
    }
   },
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
        var arg204,arg205,t,arg206,t1;
        t=PrintfHelpers.toSafe(player.Name);
        arg205=List.ofArray([Doc.TextNode(t)]);
        t1=Strings.PadLeft(Global.String(player.Ping),3);
        arg206=List.ofArray([Doc.TextNode(t1)]);
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
     var arg00,rvState,rvAvailable,rvMessage,loginSection,platoonSelection,orderAssignment,arg206,arg10,_arg00_,arg101,arg207,_arg00_1,arg102,_arg00_2;
     arg00={
      User:{
       $:0
      },
      Coalition:{
       $:0
      },
      Platoons:Runtime.New(T,{
       $:0
      })
     };
     rvState=Var.Create(arg00);
     rvAvailable=Var.Create(Runtime.New(T,{
      $:0
     }));
     rvMessage=Var.Create("Welcome to Coconut's 'Ground Commander' for IL-2 Sturmovik: Battle Of Stalingrad");
     loginSection=function(state)
     {
      var matchValue,_,user,arg20,coalitionPwd,userPwd,userName,arg201,arg202,arg30,arg203,attrs;
      matchValue=state.User;
      if(matchValue.$==1)
       {
        user=matchValue.$0;
        arg20=function()
        {
         var User,Platoons;
         AjaxRemotingProvider.Send("Commander:10",[user]);
         User={
          $:0
         };
         Platoons=Runtime.New(T,{
          $:0
         });
         Var1.Set(rvState,{
          User:User,
          Coalition:{
           $:0
          },
          Platoons:Platoons
         });
         return Var1.Set(rvMessage,"");
        };
        _=Doc.Button("Log out",Runtime.New(T,{
         $:0
        }),arg20);
       }
      else
       {
        coalitionPwd=Var.Create("AAA");
        userPwd=Var.Create("1234");
        userName=Var.Create("playerName");
        arg30=function(userName1)
        {
         return AjaxRemotingProvider.Send("Commander:2",[userName1]);
        };
        arg202=List.ofArray([Doc.TextNode("Request PIN "),Doc.Input(Runtime.New(T,{
         $:0
        }),userName),Doc.ButtonView("Send PIN to my in-game chat",Runtime.New(T,{
         $:0
        }),userName.get_View(),arg30)]);
        attrs=Runtime.New(T,{
         $:0
        });
        arg203=List.ofArray([Doc.TextNode("Your coalition code (3 letters) and your PIN (4 digits):"),Doc.Input(Runtime.New(T,{
         $:0
        }),coalitionPwd),Doc.Input(Runtime.New(T,{
         $:0
        }),userPwd),Doc.TextNode(" "),Doc.Button("Log in",attrs,function()
        {
         var arg001;
         arg001=Concurrency.Delay(function()
         {
          var x;
          x=AjaxRemotingProvider.Async("Commander:3",[Var1.Get(userPwd),Var1.Get(coalitionPwd).toUpperCase()]);
          return Concurrency.Bind(x,function(_arg1)
          {
           var patternInput,_1,username,user1,coalition,_2,user2,status,coalition1,platoons;
           if(_arg1.$==0)
            {
             _1=[{
              $:0
             },{
              $:0
             },"Incorrect coalition code or PIN"];
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
             },"Welcome "+PrintfHelpers.toSafe(username)+" in coalition "+PrintfHelpers.toSafe(_2)];
            }
           patternInput=_1;
           user2=patternInput[0];
           status=patternInput[2];
           coalition1=patternInput[1];
           platoons=coalition1.$==0?Runtime.New(T,{
            $:0
           }):coalition1.$0.$==0?axisPlatoons:alliedPlatoons;
           return Concurrency.Bind(AjaxRemotingProvider.Async("Commander:5",[platoons]),function(_arg2)
           {
            Var1.Set(rvState,{
             User:user2,
             Coalition:coalition1,
             Platoons:state.Platoons
            });
            Var1.Set(rvAvailable,_arg2);
            Var1.Set(rvMessage,status);
            return Concurrency.Return(null);
           });
          });
         });
         return Concurrency.Start(arg001,{
          $:0
         });
        })]);
        arg201=List.ofArray([Doc.Element("div",[],arg202),Doc.Element("div",[],arg203)]);
        _=Doc.Element("div",[],arg201);
       }
      return _;
     };
     platoonSelection=function(state)
     {
      return function(available)
      {
       var matchValue,_,_1,coalition,user,_2,first,chosen,arg20,attrs,view,arg201;
       matchValue=[state.User,state.Coalition];
       if(matchValue[1].$==1)
        {
         if(matchValue[0].$==1)
          {
           coalition=matchValue[1].$0;
           user=matchValue[0].$0;
           if(available.$==1)
            {
             first=available.$0;
             chosen=Var.Create(first);
             attrs=Runtime.New(T,{
              $:0
             });
             view=chosen.get_View();
             arg20=List.ofArray([Doc.Button("Update",Runtime.New(T,{
              $:0
             }),function()
             {
              var arg001;
              arg001=Concurrency.Delay(function()
              {
               var platoons;
               platoons=coalition.$==0?axisPlatoons:alliedPlatoons;
               return Concurrency.Bind(AjaxRemotingProvider.Async("Commander:5",[platoons]),function(_arg3)
               {
                Var1.Set(rvAvailable,_arg3);
                return Concurrency.Return(null);
               });
              });
              return Concurrency.Start(arg001,{
               $:0
              });
             }),Doc.TextNode(" "),Doc.Select(Runtime.New(T,{
              $:0
             }),function(x)
             {
              return x.AsString();
             },available,chosen),Doc.TextNode(" "),Doc.ButtonView("Add",attrs,view,function(platoon)
             {
              var arg001;
              arg001=Concurrency.Delay(function()
              {
               var x;
               x=AjaxRemotingProvider.Async("Commander:6",[user,platoon]);
               return Concurrency.Bind(x,function(_arg4)
               {
                var _3,Platoons;
                if(_arg4)
                 {
                  Platoons=Runtime.New(T,{
                   $:1,
                   $0:platoon,
                   $1:state.Platoons
                  });
                  Var1.Set(rvState,{
                   User:state.User,
                   Coalition:state.Coalition,
                   Platoons:Platoons
                  });
                  Var1.Set(rvMessage,"Platoon grabbed: "+PrintfHelpers.toSafe(platoon.AsString()));
                  _3=Concurrency.Return(null);
                 }
                else
                 {
                  Var1.Set(rvMessage,"Could not grab control of "+PrintfHelpers.toSafe(platoon.AsString()));
                  _3=Concurrency.Return(null);
                 }
                return _3;
               });
              });
              return Concurrency.Start(arg001,{
               $:0
              });
             })]);
             _2=Doc.Element("div",[],arg20);
            }
           else
            {
             arg201=List.ofArray([Doc.TextNode("All platoons assigned")]);
             _2=Doc.Element("div",[],arg201);
            }
           _1=_2;
          }
         else
          {
           _1=Doc.get_Empty();
          }
         _=_1;
        }
       else
        {
         _=Doc.get_Empty();
        }
       return _;
      };
     };
     orderAssignment=function(state)
     {
      var mapping,x,folder,state1,wpRank,compressDestination,projection,mapping1,list,moveTo,orderList,speedList,fireList,matchValue,_,user,tryGiveOrder,mkRow,arg205;
      mapping=function(i)
      {
       return function(wp)
       {
        return[wp,i];
       };
      };
      x=List.mapi(mapping,waypoints);
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
      wpRank=Seq.fold(folder,state1,x);
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
      matchValue=state.User;
      if(matchValue.$==0)
       {
        _=Doc.get_Empty();
       }
      else
       {
        user=matchValue.$0;
        tryGiveOrder=function(platoon)
        {
         return function(orderAndPolicy)
         {
          return Concurrency.Delay(function()
          {
           var x1;
           x1=AjaxRemotingProvider.Async("Commander:8",[user,platoon]);
           return Concurrency.Bind(x1,function(_arg6)
           {
            var _1,orderString,orderDesc;
            if(_arg6)
             {
              orderString=orderAndPolicy.ToServerInput(platoon.AsString(),compressDestination);
              orderDesc=orderAndPolicy.Description(platoon.AsString());
              AjaxRemotingProvider.Send("Commander:0",[orderString,user,orderDesc]);
              Var1.Set(rvMessage,orderDesc);
              _1=Concurrency.Return(null);
             }
            else
             {
              Var1.Set(rvMessage,"Failed to send order");
              _1=Concurrency.Return(null);
             }
            return _1;
           });
          });
         };
        };
        mkRow=function(platoon)
        {
         var chosenOrder,chosenDestination,chosenSpeed,chosenFire,arg20,attrs,arg201,arg202,arg203,arg204;
         chosenOrder=Var.Create(List.head(orderList));
         chosenDestination=Var.Create(List.head(moveTo));
         chosenSpeed=Var.Create(List.head(speedList));
         chosenFire=Var.Create(List.head(fireList));
         attrs=Runtime.New(T,{
          $:0
         });
         arg201=function()
         {
          var x1,arg001;
          x1=Runtime.New(OrderAndPolicy,{
           $:0,
           $0:Var1.Get(chosenOrder),
           $1:{
            $:0
           }
          });
          arg001=(tryGiveOrder(platoon))(x1);
          return Concurrency.Start(arg001,{
           $:0
          });
         };
         arg202=function()
         {
          var x1,arg001;
          x1=Runtime.New(OrderAndPolicy,{
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
          arg001=(tryGiveOrder(platoon))(x1);
          return Concurrency.Start(arg001,{
           $:0
          });
         };
         arg203=function()
         {
          var x1,arg001;
          x1=Runtime.New(OrderAndPolicy,{
           $:0,
           $0:Runtime.New(Order,{
            $:0
           }),
           $1:{
            $:0
           }
          });
          arg001=(tryGiveOrder(platoon))(x1);
          return Concurrency.Start(arg001,{
           $:0
          });
         };
         arg204=function()
         {
          var x1,arg001;
          x1=Runtime.New(OrderAndPolicy,{
           $:0,
           $0:Runtime.New(Order,{
            $:1
           }),
           $1:{
            $:0
           }
          });
          arg001=(tryGiveOrder(platoon))(x1);
          return Concurrency.Start(arg001,{
           $:0
          });
         };
         arg20=List.ofArray([Doc.Button("X",attrs,function()
         {
          var arg001;
          arg001=Concurrency.Delay(function()
          {
           var predicate,list1,underControl,matchValue1,platoons;
           AjaxRemotingProvider.Send("Commander:7",[user,platoon]);
           predicate=function(y)
           {
            return!Unchecked.Equals(platoon,y);
           };
           list1=state.Platoons;
           underControl=List.filter(predicate,list1);
           matchValue1=state.Coalition;
           platoons=matchValue1.$==0?Runtime.New(T,{
            $:0
           }):matchValue1.$0.$==0?axisPlatoons:alliedPlatoons;
           return Concurrency.Bind(AjaxRemotingProvider.Async("Commander:5",[platoons]),function(_arg7)
           {
            Var1.Set(rvState,{
             User:state.User,
             Coalition:state.Coalition,
             Platoons:underControl
            });
            Var1.Set(rvAvailable,_arg7);
            return Concurrency.Return(null);
           });
          });
          return Concurrency.Start(arg001,{
           $:0
          });
         }),Doc.TextNode(" "),Doc.TextNode(platoon.AsString()),Doc.TextNode(" "),Doc.Select(Runtime.New(T,{
          $:0
         }),function(arg001)
         {
          return Order.Show(arg001);
         },orderList,chosenOrder),Doc.TextNode(" "),Doc.Button("Order",Runtime.New(T,{
          $:0
         }),arg201),Doc.TextNode(" - "),Doc.TextNode("Move towards..."),Doc.Select(Runtime.New(T,{
          $:0
         }),function(arg001)
         {
          return Order.Show(arg001);
         },moveTo,chosenDestination),Doc.TextNode(" at "),Doc.Select(Runtime.New(T,{
          $:0
         }),function(arg001)
         {
          return Speed.Show(arg001);
         },speedList,chosenSpeed),Doc.TextNode(", "),Doc.Select(Runtime.New(T,{
          $:0
         }),function(arg001)
         {
          return FireControl.Show(arg001);
         },fireList,chosenFire),Doc.Button("Move",Runtime.New(T,{
          $:0
         }),arg202),Doc.TextNode(" - "),Doc.Button("Stop",Runtime.New(T,{
          $:0
         }),arg203),Doc.TextNode(" "),Doc.Button("Continue",Runtime.New(T,{
          $:0
         }),arg204)]);
         return Doc.Element("div",[],arg20);
        };
        arg205=Seq.toList(Seq.delay(function()
        {
         return Seq.map(function(platoon)
         {
          return mkRow(platoon);
         },state.Platoons);
        }));
        _=Doc.Element("div",[],arg205);
       }
      return _;
     };
     arg10=rvState.get_View();
     _arg00_=View.Map(loginSection,arg10);
     arg101=rvState.get_View();
     arg207=rvAvailable.get_View();
     _arg00_1=View.Map2(platoonSelection,arg101,arg207);
     arg102=rvState.get_View();
     _arg00_2=View.Map(orderAssignment,arg102);
     arg206=List.ofArray([Doc.TextView(rvMessage.get_View()),Doc.EmbedView(_arg00_),Doc.EmbedView(_arg00_1),Doc.EmbedView(_arg00_2)]);
     return Doc.Element("div",[],arg206);
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
  Commander=Runtime.Safe(Global.Commander);
  GameEvents=Runtime.Safe(Commander.GameEvents);
  State=Runtime.Safe(GameEvents.State);
  UI=Runtime.Safe(Global.WebSharper.UI);
  Next=Runtime.Safe(UI.Next);
  Var=Runtime.Safe(Next.Var);
  Doc=Runtime.Safe(Next.Doc);
  List=Runtime.Safe(Global.WebSharper.List);
  T=Runtime.Safe(List.T);
  Concurrency=Runtime.Safe(Global.WebSharper.Concurrency);
  Remoting=Runtime.Safe(Global.WebSharper.Remoting);
  AjaxRemotingProvider=Runtime.Safe(Remoting.AjaxRemotingProvider);
  Var1=Runtime.Safe(Next.Var1);
  View=Runtime.Safe(Next.View);
  PrintfHelpers=Runtime.Safe(Global.WebSharper.PrintfHelpers);
  WebCommander=Runtime.Safe(Commander.WebCommander);
  OrderAndPolicy=Runtime.Safe(WebCommander.OrderAndPolicy);
  Order=Runtime.Safe(WebCommander.Order);
  Speed=Runtime.Safe(WebCommander.Speed);
  FireControl=Runtime.Safe(WebCommander.FireControl);
  Seq=Runtime.Safe(Global.WebSharper.Seq);
  Strings=Runtime.Safe(Global.WebSharper.Strings);
  Collections=Runtime.Safe(Global.WebSharper.Collections);
  FSharpMap=Runtime.Safe(Collections.FSharpMap);
  return Unchecked=Runtime.Safe(Global.WebSharper.Unchecked);
 });
 Runtime.OnLoad(function()
 {
  return;
 });
}());
