// Pure UI-generator tests, not a browser/layout test. No packages, network or game mutations.
const fs=require('fs'),vm=require('vm'),path=require('path');
const root=path.resolve(__dirname,'..'),assets=path.join(root,'Assets/StreamingAssets/NsoWiki');
const nodes=new Map();
for(const id of ['toast','sidebar','navigation','crumb','pageTitle','pageDescription','eyebrow','export','search','notice','refresh','navToggle','clearSearch','content','connection','dataTime'])
  nodes.set(id,{value:'',textContent:'',innerHTML:'',hidden:false,disabled:false,className:'',classList:{toggle(){}},addEventListener(){},focus(){}});
const context=vm.createContext({console,URL,URLSearchParams,Blob,setTimeout:()=>0,clearTimeout(){},setInterval:()=>0,
  location:{hash:'',href:'http://127.0.0.1:8787/'},navigator:{clipboard:{writeText(){return Promise.resolve();}}},
  localStorage:{getItem(){return null;},setItem(){}},
  document:{hidden:false,activeElement:{tagName:'BODY'},getElementById:id=>nodes.get(id)||null,addEventListener(){}},
  window:{addEventListener(){}},fetch(){return Promise.reject(Error('Network disabled in pure UI tests'));}});
const source=fs.readFileSync(path.join(assets,'app.js'),'utf8').replace(/load\(\);pollTimer=setInterval\(poll,2000\);\s*$/,'');
vm.runInContext(source,context,{filename:'app.js'});
context.fixture=JSON.parse(fs.readFileSync(path.join(root,'NsoWiki/db.json'),'utf8'));
vm.runInContext('DB=fixture;runtime={players:[]};control={revision:0,rules:{blockTrainingDummy:true,blockOverlevel:true,overlevelGap:10,limitOrdinaryLoot:true,ordinaryMaxGap:7},defaults:{}};',context);
let count=0;
function check(expression,label){if(!vm.runInContext(expression,context))throw Error('FAIL '+label);console.log('PASS '+label);count++;}
check('fold("Kiếm gỗ Đỏ")==="kiem go do"','accent-insensitive Vietnamese lookup');
check('fieldMatch(194,"194")&&!fieldMatch(194,"19")','numeric filters match exact IDs');
check('fieldMatch([{mapName:"Làng Tone"}],"lang tone")','filters inspect nested references');
check('esc("<img src=x onerror=alert(1)>").includes("&lt;")','record names escape HTML');
check('queryTerms(\'status:partial giverNpc:12 name:"kiem go"\').length===3','quoted field queries parse correctly');
check('q="id:2";statusFilter="";facet="";matching("quests").length===1','real quest data supports exact ID search');
check('q="";detail("quests",2).includes("#mobs/0")&&detail("quests",2).includes("Tiến trình nhiệm vụ")','quest detail links its real dummy target');
check('Object.keys(DB.collections).every(c=>{routeName=c;selected=null;q="";statusFilter="";facet="";return library().includes("data-table");})','all 17 data collections generate browsable tables');
check('Object.keys(DB.collections).every(c=>typeof detail(c,DB.collections[c][0].id)==="string")','all 17 collection types generate details');
check('location.hash="#quests/2";readRoute();routeName==="quests"&&selected==="2"','original deep links remain compatible');
check('location.hash="#__proto__";readRoute();routeName==="overview"','unknown/prototype hashes cannot break routing');
check('settings().includes("rulesForm")&&settings().includes("ordinaryMaxGap")&&settings().includes("Lưu & áp dụng")','settings show both independent gates and explicit save');
check('inspector().includes("inspectForm")&&inspector().includes("questStep")','loot inspector includes quest-step context');
check('overview().includes("Phiên chơi hiện tại")&&overview().includes("Chưa có nhân vật")','dashboard clearly represents an empty live world');
check('!validSavedKey("javascript:alert(1)")&&!validSavedKey("quests/%broken")&&validSavedKey("quests/2")','damaged bookmark storage cannot break routing');
check('activity([{revision:1,time:"2026-11-11T00:00:00Z",before:{blockTrainingDummy:true},after:{blockTrainingDummy:false}}]).includes("Chặn bù nhìn")','change log displays human-readable before/after settings');
check('DB.collections.items.push({id:999999,name:"<script>alert(1)</script>"});!detail("items",999999).includes("<script>alert")','detail and raw JSON panels escape untrusted text');
check('runtime.players=[{id:1,name:"Test",level:20,hp:50,maxHp:100,mp:75,maxMp:100,mapId:22,mapName:"Làng Tone",zone:0,x:100,y:200,questId:2,questName:"Kiếm gỗ",questActive:true,stepName:"Hạ bù nhìn",stepCount:3,stepTarget:10,yen:100,xu:0,bag:[{id:194,name:"Kiếm gỗ",quantity:1}],bagCells:30}];playerCards().includes("#quests/2")&&playerCards().includes("#items/194")&&playerCards().includes("3/10")','live cards connect quest progress, maps and inventory');
console.log(`${count}/18 pure UI checks passed; browser visual QA still required.`);
