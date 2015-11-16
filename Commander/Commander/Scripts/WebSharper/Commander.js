(function()
{
 var Global=this,Runtime=this.IntelliFactory.Runtime,UI,Next,Var,List,Doc,T,Concurrency,Remoting,AjaxRemotingProvider,Var1,PrintfHelpers,Commander,WebCommander,OrderAndPolicy,Order,Speed,FireControl,Seq,Collections,FSharpMap,Operators;
 Runtime.Define(Global,{
  Commander:{
   WebCommander:{
    FireControl:Runtime.Class({},{
     Show:function(fire)
     {
      return fire.$==1?"return fire only":fire.$==2?"hold fire":"free fire";
     }
    }),
    Login:function()
    {
     var password,status,arg20,arg201;
     password=Var.Create("AAA1234");
     status=Var.Create("Please enter the pin code provided to you in the game chat");
     arg201=List.ofArray([Doc.TextNode("Your password: "),Doc.Input(Runtime.New(T,{
      $:0
     }),password),Doc.Button("Log in",Runtime.New(T,{
      $:0
     }),function()
     {
      return Concurrency.Start(Concurrency.Delay(function()
      {
       return Concurrency.Bind(AjaxRemotingProvider.Async("Commander:2",[Var1.Get(password)]),function(_arg1)
       {
        if(_arg1.$==0)
         {
          Var1.Set(status,"Incorrect pin code");
          return Concurrency.Return(null);
         }
        else
         {
          Var1.Set(status,"Welcome "+PrintfHelpers.toSafe(_arg1.$0));
          return Concurrency.Return(null);
         }
       });
      }),{
       $:0
      });
     })]);
     arg20=List.ofArray([Doc.Element("div",[],arg201),Doc.TextView(status.get_View())]);
     return Doc.Element("div",[],arg20);
    },
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
      var arg201,arg202,arg203,arg204;
      arg202=List.ofArray([Doc.TextNode("Client ID")]);
      arg203=List.ofArray([Doc.TextNode("Status")]);
      arg204=List.ofArray([Doc.TextNode("Name")]);
      arg201=List.ofArray([Doc.Element("td",[],arg202),Doc.Element("td",[],arg203),Doc.Element("td",[],arg204)]);
      return Seq.append([Doc.Element("tr",[],arg201)],Seq.delay(function()
      {
       return Seq.map(function(player)
       {
        var arg205,arg206,t,arg207,t1,arg208,t2;
        t=PrintfHelpers.padNumLeft(Global.String(player.ClientId),2);
        arg206=List.ofArray([Doc.TextNode(t)]);
        t1=Global.String(player.Status);
        arg207=List.ofArray([Doc.TextNode(t1)]);
        t2=PrintfHelpers.toSafe(player.Name);
        arg208=List.ofArray([Doc.TextNode(t2)]);
        arg205=List.ofArray([Doc.Element("td",[],arg206),Doc.Element("td",[],arg207),Doc.Element("td",[],arg208)]);
        return Doc.Element("tr",[],arg205);
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
     var mapping,x,folder,state,wpRank,compressDestination,projection,mapping1,list,moveTo,orderList,speedList,fireList,mkRow,numRows,arg205;
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
     state=FSharpMap.New1([]);
     wpRank=Seq.fold(folder,state,x);
     compressDestination=function(wp)
     {
      return"WP"+Global.String(wpRank.get_Item(wp));
     };
     projection=function(_arg1)
     {
      return _arg1==="North"?[0,{
       $:0
      }]:_arg1==="North-East"?[1,{
       $:0
      }]:_arg1==="East"?[2,{
       $:0
      }]:_arg1==="South-East"?[3,{
       $:0
      }]:_arg1==="South"?[4,{
       $:0
      }]:_arg1==="South-West"?[5,{
       $:0
      }]:_arg1==="West"?[6,{
       $:0
      }]:_arg1==="North-West"?[7,{
       $:0
      }]:[8,{
       $:1,
       $0:_arg1
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
     }),Runtime.New(Order,{
      $:5
     }),Runtime.New(Order,{
      $:6
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
     mkRow=function(defaultPlatoon)
     {
      var chosenOrder,chosenDestination,chosenSpeed,chosenFire,chosenPlatoon,arg20,arg201,arg202,arg203,arg204;
      chosenOrder=Var.Create(List.head(orderList));
      chosenDestination=Var.Create(List.head(moveTo));
      chosenSpeed=Var.Create(List.head(speedList));
      chosenFire=Var.Create(List.head(fireList));
      chosenPlatoon=Var.Create(Seq.nth(defaultPlatoon,platoons));
      arg201=function()
      {
       var orderAndPolicy;
       orderAndPolicy=Runtime.New(OrderAndPolicy,{
        $:0,
        $0:Var1.Get(chosenOrder),
        $1:{
         $:0
        }
       });
       return AjaxRemotingProvider.Send("Commander:0",[orderAndPolicy.ToServerInput(Var1.Get(chosenPlatoon),compressDestination)]);
      };
      arg202=function()
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
       return AjaxRemotingProvider.Send("Commander:0",[orderAndPolicy.ToServerInput(Var1.Get(chosenPlatoon),compressDestination)]);
      };
      arg203=function()
      {
       return AjaxRemotingProvider.Send("Commander:0",[Runtime.New(OrderAndPolicy,{
        $:0,
        $0:Runtime.New(Order,{
         $:0
        }),
        $1:{
         $:0
        }
       }).ToServerInput(Var1.Get(chosenPlatoon),compressDestination)]);
      };
      arg204=function()
      {
       return AjaxRemotingProvider.Send("Commander:0",[Runtime.New(OrderAndPolicy,{
        $:0,
        $0:Runtime.New(Order,{
         $:1
        }),
        $1:{
         $:0
        }
       }).ToServerInput(Var1.Get(chosenPlatoon),compressDestination)]);
      };
      arg20=List.ofArray([Doc.Select(Runtime.New(T,{
       $:0
      }),function(x1)
      {
       return x1;
      },platoons,chosenPlatoon),Doc.Select(Runtime.New(T,{
       $:0
      }),function(arg00)
      {
       return Order.Show(arg00);
      },orderList,chosenOrder),Doc.Button("Order",Runtime.New(T,{
       $:0
      }),arg201),Doc.TextNode(" - "),Doc.TextNode("Move towards..."),Doc.Select(Runtime.New(T,{
       $:0
      }),function(arg00)
      {
       return Order.Show(arg00);
      },moveTo,chosenDestination),Doc.TextNode(" at "),Doc.Select(Runtime.New(T,{
       $:0
      }),function(arg00)
      {
       return Speed.Show(arg00);
      },speedList,chosenSpeed),Doc.TextNode(", "),Doc.Select(Runtime.New(T,{
       $:0
      }),function(arg00)
      {
       return FireControl.Show(arg00);
      },fireList,chosenFire),Doc.Button("Move",Runtime.New(T,{
       $:0
      }),arg202),Doc.TextNode(" - "),Doc.Button("Stop",Runtime.New(T,{
       $:0
      }),arg203),Doc.TextNode(" "),Doc.Button("Continue",Runtime.New(T,{
       $:0
      }),arg204)]);
      return Doc.Element("div",[],arg20);
     };
     numRows=Operators.Min(Seq.length(platoons),6);
     arg205=Seq.toList(Seq.delay(function()
     {
      return Seq.map(function(i)
      {
       return mkRow(i);
      },Operators.range(0,numRows-1));
     }));
     return Doc.Element("div",[],arg205);
    }
   }
  }
 });
 Runtime.OnInit(function()
 {
  UI=Runtime.Safe(Global.WebSharper.UI);
  Next=Runtime.Safe(UI.Next);
  Var=Runtime.Safe(Next.Var);
  List=Runtime.Safe(Global.WebSharper.List);
  Doc=Runtime.Safe(Next.Doc);
  T=Runtime.Safe(List.T);
  Concurrency=Runtime.Safe(Global.WebSharper.Concurrency);
  Remoting=Runtime.Safe(Global.WebSharper.Remoting);
  AjaxRemotingProvider=Runtime.Safe(Remoting.AjaxRemotingProvider);
  Var1=Runtime.Safe(Next.Var1);
  PrintfHelpers=Runtime.Safe(Global.WebSharper.PrintfHelpers);
  Commander=Runtime.Safe(Global.Commander);
  WebCommander=Runtime.Safe(Commander.WebCommander);
  OrderAndPolicy=Runtime.Safe(WebCommander.OrderAndPolicy);
  Order=Runtime.Safe(WebCommander.Order);
  Speed=Runtime.Safe(WebCommander.Speed);
  FireControl=Runtime.Safe(WebCommander.FireControl);
  Seq=Runtime.Safe(Global.WebSharper.Seq);
  Collections=Runtime.Safe(Global.WebSharper.Collections);
  FSharpMap=Runtime.Safe(Collections.FSharpMap);
  return Operators=Runtime.Safe(Global.WebSharper.Operators);
 });
 Runtime.OnLoad(function()
 {
  return;
 });
}());
