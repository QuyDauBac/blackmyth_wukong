// Cinematic full-screen navigation and study interactions.
// Account calls target the ASP.NET MVC controllers and include the antiforgery token.
const viewport=document.querySelector("#page-viewport");
const track=document.querySelector("#page-track");
const screens=[...document.querySelectorAll(".screen")];
const sideNav=document.querySelector("#side-nav");
const menuToggle=document.querySelector("#menu-toggle");
const pageProgress=document.querySelector(".scroll-progress");
const reducedMotion=window.matchMedia("(prefers-reduced-motion: reduce)").matches;
const assetBase=window.WUKONG_BASE||"/";
let activeScreen=0;
let lastNavigationAt=0;
let overviewIndex=0;
let overviewTimer=0;
let mediaIndex=0;
let mediaTimer=0;
let touchStartY=0;
let toastTimer=0;

const mediaItems=[
  {title:"The journey begins",poster:assetBase+"assets/img_art_video_1.431a2a74.png"},
  {title:"The Destined One",poster:assetBase+"assets/img_art_video_2.479826b5.png"},
  {title:"A world of wonders",poster:assetBase+"assets/img_art_video_3.c243b5a0.png"},
  {title:"A legendary foe",poster:assetBase+"assets/img_art_video_4.2c352fb9.png"},
  {title:"Stories untold",poster:assetBase+"assets/img_art_video_5.2059e501.png"}
];
const mediaVideo=document.querySelector("#media-player");
const mediaOverlay=document.querySelector("#media-player-overlay");
const footerVideo=document.querySelector(".footer-video");
const purchaseOptions=document.querySelector("#purchase-options");
const overviewSlides=[...document.querySelectorAll(".overview-slide")];

function showToast(message){
  const toast=document.querySelector("#toast");
  toast.textContent=message;
  toast.classList.add("is-visible");
  clearTimeout(toastTimer);
  toastTimer=setTimeout(()=>toast.classList.remove("is-visible"),2800);
}
function updateScreenHeight(){
  document.documentElement.style.setProperty("--screen-height",window.innerHeight+"px");
  moveTrack(false);
}
function moveTrack(animate=true){
  if(!animate)track.style.transition="none";
  track.style.transform="translate3d(0,"+(-activeScreen*window.innerHeight)+"px,0)";
  document.documentElement.style.setProperty("--screen-index",String(activeScreen));
  document.documentElement.style.setProperty("--page-progress",String((activeScreen+(activeScreen===2?overviewIndex/overviewSlides.length:activeScreen===3?mediaIndex/mediaItems.length:0))/(screens.length-1)));
  if(!animate){requestAnimationFrame(()=>requestAnimationFrame(()=>track.style.removeProperty("transition")));}
}
function setActiveScreen(index,force=false){
  const next=Math.max(0,Math.min(screens.length-1,index));
  const now=Date.now();
  if(next===activeScreen&&!force){menuToggle.setAttribute("aria-expanded","false");menuToggle.setAttribute("aria-label","Open navigation");sideNav.classList.remove("is-open");return;}
  if(!force&&now-lastNavigationAt<500)return;
  lastNavigationAt=now;
  activeScreen=next;
  screens.forEach((screen,i)=>{
    const active=i===activeScreen;
    screen.classList.toggle("is-active",active);
    screen.setAttribute("aria-hidden",String(!active));
  });
  document.querySelectorAll("[data-screen-target]").forEach(control=>{
    if(control.classList.contains("screen-dot")){
      const active=Number(control.dataset.screenTarget)===activeScreen;
      control.classList.toggle("is-active",active);
      if(active)control.setAttribute("aria-current","step");else control.removeAttribute("aria-current");
    }
  });
  sideNav.querySelectorAll("a").forEach(link=>link.classList.toggle("is-active",Number(link.dataset.screenTarget)===activeScreen));
  menuToggle.setAttribute("aria-expanded","false");
  menuToggle.setAttribute("aria-label","Open navigation");
  sideNav.classList.remove("is-open");
  const buyButton=document.querySelector("#buy-floating");
  if(buyButton)buyButton.classList.toggle("is-hidden",activeScreen===4);
  moveTrack(true);
  syncCarousels();
  if(activeScreen!==3&&mediaVideo&&!mediaVideo.paused)mediaVideo.pause();
  if(activeScreen===5&&footerVideo&&!reducedMotion){footerVideo.play().catch(()=>{});}else if(footerVideo)footerVideo.pause();
}
function syncCarousels(){
  clearInterval(overviewTimer);overviewTimer=0;
  clearInterval(mediaTimer);mediaTimer=0;
  if(reducedMotion||window.innerWidth<=900)return;
  if(activeScreen===2)overviewTimer=setInterval(()=>setOverviewSlide(overviewIndex+1),5000);
  if(activeScreen===3&&!mediaVideo.paused) return;
  if(activeScreen===3)mediaTimer=setInterval(()=>setMediaSlide(mediaIndex+1),5000);
}
function setOverviewSlide(index){
  overviewIndex=(index+overviewSlides.length)%overviewSlides.length;
  overviewSlides.forEach((slide,i)=>{
    const active=i===overviewIndex;
    slide.classList.toggle("is-active",active);
    slide.setAttribute("aria-hidden",String(!active));
  });
  document.querySelector("#overview-count").innerHTML=String(overviewIndex+1).padStart(2,"0")+" <i>/</i> "+String(overviewSlides.length).padStart(2,"0");
  document.documentElement.style.setProperty("--page-progress",String((activeScreen+overviewIndex/overviewSlides.length)/(screens.length-1)));
}
function setMediaSlide(index){
  mediaIndex=(index+mediaItems.length)%mediaItems.length;
  const item=mediaItems[mediaIndex];
  mediaVideo.poster=item.poster;
  document.querySelector("#media-current-title").textContent=item.title;
  document.querySelector("#media-count").textContent=String(mediaIndex+1).padStart(2,"0");
  document.querySelectorAll(".media-thumb").forEach((thumb,i)=>{
    const active=i===mediaIndex;
    thumb.classList.toggle("is-active",active);
    if(active)thumb.setAttribute("aria-current","true");else thumb.removeAttribute("aria-current");
  });
  document.documentElement.style.setProperty("--page-progress",String((activeScreen+mediaIndex/mediaItems.length)/(screens.length-1)));
}
function navigateFromInput(direction){setActiveScreen(activeScreen+direction);}

// One screen per wheel gesture, with a 500 ms input lock and one-second CSS transition.
window.addEventListener("wheel",event=>{
  if(document.body.classList.contains("modal-open")||purchaseOptions.classList.contains("is-open")||event.target.closest("input,textarea,select,.account-modal,.language-menu"))return;
  if(Math.abs(event.deltaY)<18)return;
  event.preventDefault();
  const now=Date.now();
  if(now-lastNavigationAt<500)return;
  navigateFromInput(event.deltaY>0?1:-1);
},{passive:false});
viewport.addEventListener("touchstart",event=>{touchStartY=event.changedTouches[0].clientY;},{passive:true});
viewport.addEventListener("touchend",event=>{
  if(document.body.classList.contains("modal-open")||purchaseOptions.classList.contains("is-open"))return;
  const delta=touchStartY-event.changedTouches[0].clientY;
  if(Math.abs(delta)>48)navigateFromInput(delta>0?1:-1);
},{passive:true});
window.addEventListener("keydown",event=>{
  if(document.body.classList.contains("modal-open")||purchaseOptions.classList.contains("is-open"))return;
  if(["INPUT","TEXTAREA","SELECT"].includes(document.activeElement?.tagName))return;
  if(event.key==="ArrowDown"||event.key==="PageDown"){event.preventDefault();navigateFromInput(1);}
  else if(event.key===" "){if(event.target.closest("button,a"))return;event.preventDefault();navigateFromInput(1);}
  else if(event.key==="ArrowUp"||event.key==="PageUp"){event.preventDefault();navigateFromInput(-1);}
  else if(event.key==="Home"){event.preventDefault();setActiveScreen(0);}
  else if(event.key==="End"){event.preventDefault();setActiveScreen(screens.length-1);}
});
window.addEventListener("resize",()=>{updateScreenHeight();syncCarousels();},{passive:true});
document.querySelectorAll("[data-screen-target]").forEach(control=>control.addEventListener("click",event=>{
  event.preventDefault();
  setActiveScreen(Number(control.dataset.screenTarget));
}));
menuToggle.addEventListener("click",()=>{
  const open=menuToggle.getAttribute("aria-expanded")!=="true";
  menuToggle.setAttribute("aria-expanded",String(open));
  menuToggle.setAttribute("aria-label",open?"Close navigation":"Open navigation");
  sideNav.classList.toggle("is-open",open);
});

// Overview carousel: desktop autoplay every five seconds; manual controls remain on mobile.
document.querySelector("#overview-prev").addEventListener("click",()=>setOverviewSlide(overviewIndex-1));
document.querySelector("#overview-next").addEventListener("click",()=>setOverviewSlide(overviewIndex+1));
document.querySelectorAll("[data-open-media]").forEach(button=>button.addEventListener("click",()=>{
  setMediaSlide(Number(button.dataset.openMedia));
  setActiveScreen(3);
}));

// Media carousel updates the local video poster and title. The large video is loaded only on play.
document.querySelectorAll(".media-thumb").forEach(button=>button.addEventListener("click",()=>setMediaSlide(Number(button.dataset.mediaIndex))));
document.querySelector("#media-play").addEventListener("click",()=>{
  mediaVideo.play().catch(()=>showToast("The trailer could not be played. Check that the local media file is available."));
});
mediaVideo.addEventListener("play",()=>{mediaOverlay.classList.add("is-hidden");document.querySelector(".media-player-wrap").classList.remove("is-loading");clearInterval(mediaTimer);mediaTimer=0;});
mediaVideo.addEventListener("waiting",()=>{if(activeScreen===3)document.querySelector(".media-player-wrap").classList.add("is-loading");});
mediaVideo.addEventListener("canplay",()=>document.querySelector(".media-player-wrap").classList.remove("is-loading"));
mediaVideo.addEventListener("pause",()=>{mediaOverlay.classList.remove("is-hidden");if(activeScreen===3&&!reducedMotion&&window.innerWidth>900&&!mediaTimer)mediaTimer=setInterval(()=>setMediaSlide(mediaIndex+1),5000);});
mediaVideo.addEventListener("ended",()=>{mediaOverlay.classList.remove("is-hidden");});

// Language menu and consent banner.
const languageToggle=document.querySelector("#language-toggle");
const languageMenu=document.querySelector("#language-menu");
languageToggle.addEventListener("click",()=>{
  const open=languageToggle.getAttribute("aria-expanded")!=="true";
  languageToggle.setAttribute("aria-expanded",String(open));
  languageMenu.hidden=!open;
});
languageMenu.querySelectorAll("button").forEach(button=>button.addEventListener("click",()=>{
  languageToggle.innerHTML=button.dataset.language==="English"?"EN <span aria-hidden=\"true\">⌄</span>":button.dataset.language+" <span aria-hidden=\"true\">⌄</span>";
  languageToggle.setAttribute("aria-expanded","false");
  languageMenu.hidden=true;
  showToast("Language selection is a visual study control; this page currently uses English.");
}));
document.addEventListener("click",event=>{
  if(!event.target.closest(".language-control")){languageToggle.setAttribute("aria-expanded","false");languageMenu.hidden=true;}
});
const cookieBanner=document.querySelector("#cookie-banner");
try{if(localStorage.getItem("wukong-cookie-accepted")==="1")cookieBanner.hidden=true;}catch{}
document.querySelector("#cookie-accept").addEventListener("click",()=>{
  cookieBanner.hidden=true;
  try{localStorage.setItem("wukong-cookie-accepted","1");}catch{}
});
function closePurchaseOptions(){purchaseOptions.classList.remove("is-open");purchaseOptions.setAttribute("aria-hidden","true");}
function openPurchaseOptions(){purchaseOptions.classList.add("is-open");purchaseOptions.setAttribute("aria-hidden","false");document.querySelector("#purchase-options-close").focus();}
document.querySelector("#buy-floating").addEventListener("click",openPurchaseOptions);
document.querySelector("#purchase-options-close").addEventListener("click",closePurchaseOptions);
purchaseOptions.addEventListener("click",event=>{if(event.target===purchaseOptions)closePurchaseOptions();});
document.querySelector("#buy-on-web").addEventListener("click",async()=>{
  closePurchaseOptions();
  try{
    const data=await requestApi("/Account/Me",{credentials:"same-origin"});
    renderAccount(data.user);
    if(data.user){window.location.assign("/Checkout");return;}
    pendingCheckout=true;
    openAccount("login");
    accountMessage.textContent="Log in or create an account to continue to checkout.";
  }catch(error){showToast(explainAccountError(error));}
});
document.querySelector("#subscribe-button").addEventListener("click",openSubscribeModal);

// Account forms post to the MVC controller; the antiforgery token lives in #csrf-form.
function explainAccountError(error){
  if(error instanceof TypeError)return "The site could not reach the server. Reload the page and try again.";
  return error.message||"Could not complete the account request.";
}
function csrfToken(){
  const field=document.querySelector("#csrf-form input[name='__RequestVerificationToken']");
  return field?field.value:"";
}
async function refreshCsrfToken(){
  try{
    const data=await requestApi("/Account/AntiForgeryToken",{credentials:"same-origin"});
    const field=document.querySelector("#csrf-form input[name='__RequestVerificationToken']");
    if(field&&data.token)field.value=data.token;
  }catch{}
}
function formBody(values){
  const body=new URLSearchParams();
  Object.keys(values).forEach(key=>body.append(key,values[key]==null?"":values[key]));
  const token=csrfToken();
  if(token)body.append("__RequestVerificationToken",token);
  return body;
}
const accountModal=document.querySelector("#account-modal");
const accountTrigger=document.querySelector("#account-trigger");
const accountForm=document.querySelector("#account-form");
const accountTabs=document.querySelector(".account-tabs");
const accountSignedIn=document.querySelector("#account-signed-in");
const accountMessage=document.querySelector("#account-message");
let accountMode="login";
let currentUser=null;
let pendingCheckout=false;
function setAccountMode(mode){
  accountMode=mode;
  document.querySelectorAll(".account-tab").forEach(tab=>{
    const active=tab.dataset.mode===mode;
    tab.classList.toggle("is-active",active);
    tab.setAttribute("aria-selected",String(active));
  });
  document.querySelectorAll(".register-only").forEach(field=>{field.hidden=mode!=="register";});
  document.querySelector("#account-password").autocomplete=mode==="register"?"new-password":"current-password";
  document.querySelector("#account-title").innerHTML=mode==="register"?"Begin your <em>journey.</em>":"Welcome, <em>Destined One.</em>";
  document.querySelector(".account-submit").textContent=mode==="register"?"Create account":"Log in";
  accountMessage.textContent="";
  accountMessage.classList.remove("is-error");
}
function renderAccount(user){
  currentUser=user||null;
  accountTrigger.textContent=currentUser?"Hi, "+currentUser.name:"Log in / Register";
  accountTrigger.setAttribute("aria-label",currentUser?"Account: "+currentUser.name:"Log in or register");
  accountForm.hidden=Boolean(currentUser);
  accountTabs.hidden=Boolean(currentUser);
  accountSignedIn.hidden=!currentUser;
  if(currentUser)document.querySelector("#account-welcome").textContent="Signed in as "+currentUser.email;
  const adminLink=document.querySelector("#account-admin-link");
  if(adminLink)adminLink.hidden=!currentUser||currentUser.role!=="Admin";
}
function openAccount(mode){
  renderAccount(currentUser);
  if(!currentUser)setAccountMode(mode||"login");
  accountModal.classList.add("is-open");
  accountModal.setAttribute("aria-hidden","false");
  document.body.classList.add("modal-open");
  const focusTarget=currentUser?document.querySelector("#account-logout"):(accountMode==="register"?document.querySelector("#account-name"):document.querySelector("#account-email"));
  focusTarget.focus();
}
function closeAccount(){
  accountModal.classList.remove("is-open");
  accountModal.setAttribute("aria-hidden","true");
  document.body.classList.remove("modal-open");
  accountTrigger.focus();
}
accountTrigger.addEventListener("click",()=>openAccount("login"));
document.querySelector("#account-close").addEventListener("click",()=>{pendingCheckout=false;closeAccount();});
accountModal.addEventListener("click",event=>{if(event.target===accountModal){pendingCheckout=false;closeAccount();}});
document.querySelectorAll(".account-tab").forEach(tab=>tab.addEventListener("click",()=>setAccountMode(tab.dataset.mode)));
document.addEventListener("keydown",event=>{if(event.key==="Escape"&&accountModal.classList.contains("is-open")){pendingCheckout=false;closeAccount();}if(event.key==="Escape"&&purchaseOptions.classList.contains("is-open"))closePurchaseOptions();if(event.key==="Escape"&&subscribeModal&&subscribeModal.classList.contains("is-open"))closeSubscribeModal();});
async function requestApi(url,options){
  const response=await fetch(url,options);
  const data=await response.json().catch(()=>({}));
  if(!response.ok)throw new Error(data.error||"The request could not be completed.");
  return data;
}
async function refreshAccount(){
  try{const data=await requestApi("/Account/Me",{credentials:"same-origin"});renderAccount(data.user);return data.user;}catch{return null;}
}
if(new URLSearchParams(location.search).get("checkout")==="login"){
  pendingCheckout=true;
  refreshAccount().then(user=>{
    history.replaceState(null,"",location.pathname);
    if(user){pendingCheckout=false;window.location.replace("/Checkout");return;}
    openAccount("login");
    accountMessage.textContent="Log in or create an account to continue to checkout.";
  });
}else refreshAccount();
accountForm.addEventListener("submit",async event=>{
  event.preventDefault();
  accountMessage.textContent="";
  accountMessage.classList.remove("is-error");
  const email=document.querySelector("#account-email").value.trim();
  const password=document.querySelector("#account-password").value;
  const name=document.querySelector("#account-name").value.trim();
  const confirm=document.querySelector("#account-confirm").value;
  if(accountMode==="register"&&password!==confirm){
    accountMessage.textContent="The passwords do not match.";
    accountMessage.classList.add("is-error");
    return;
  }
  const submit=accountForm.querySelector(".account-submit");
  submit.disabled=true;
  try{
    const payload=accountMode==="register"
      ?{DisplayName:name,Email:email,Password:password,ConfirmPassword:confirm}
      :{Email:email,Password:password};
    const endpoint=accountMode==="register"?"/Account/JsonRegister":"/Account/JsonLogin";
    const data=await requestApi(endpoint,{method:"POST",credentials:"same-origin",headers:{"Content-Type":"application/x-www-form-urlencoded;charset=UTF-8"},body:formBody(payload).toString()});
    renderAccount(data.user);
    await refreshCsrfToken();
    const continueToCheckout=pendingCheckout;
    pendingCheckout=false;
    closeAccount();
    if(continueToCheckout){window.location.assign("/Checkout");return;}
    showToast(data.existing?"You are signed in to your existing account.":(accountMode==="register"?"Your account has been created.":"Welcome back, "+data.user.name+"."));
  }catch(error){
    if(error.message.startsWith("An account with this email already exists.")){
      setAccountMode("login");
      document.querySelector("#account-email").value=email;
      accountMessage.textContent="This email already has an account. Log in with its existing password.";
      accountMessage.classList.remove("is-error");
    }else{
      accountMessage.textContent=explainAccountError(error);
      accountMessage.classList.add("is-error");
    }
  }finally{submit.disabled=false;}
});
document.querySelector("#account-logout").addEventListener("click",async()=>{
  try{
    await requestApi("/Account/JsonLogout",{method:"POST",credentials:"same-origin",headers:{"Content-Type":"application/x-www-form-urlencoded;charset=UTF-8"},body:formBody({}).toString()});
    renderAccount(null);setAccountMode("login");closeAccount();await refreshCsrfToken();showToast("You have signed out.");
  }catch(error){document.querySelector("#account-welcome").textContent=explainAccountError(error);}
});

// Newsletter: the footer button opens a small panel and posts to /Home/Subscribe,
// which inserts the address into the NewsletterSubscribers table.
const subscribeModal=document.querySelector("#subscribe-modal");
const subscribeForm=document.querySelector("#subscribe-form");
const subscribeMessage=document.querySelector("#subscribe-message");
function openSubscribeModal(){
  if(!subscribeModal)return;
  subscribeMessage.textContent="";
  subscribeMessage.classList.remove("is-error");
  subscribeModal.classList.add("is-open");
  subscribeModal.setAttribute("aria-hidden","false");
  document.body.classList.add("modal-open");
  document.querySelector("#subscribe-email").focus();
}
function closeSubscribeModal(){
  if(!subscribeModal)return;
  subscribeModal.classList.remove("is-open");
  subscribeModal.setAttribute("aria-hidden","true");
  document.body.classList.remove("modal-open");
}
if(subscribeModal&&subscribeForm){
  document.querySelector("#subscribe-close").addEventListener("click",closeSubscribeModal);
  subscribeModal.addEventListener("click",event=>{if(event.target===subscribeModal)closeSubscribeModal();});
  subscribeForm.addEventListener("submit",async event=>{
    event.preventDefault();
    const email=document.querySelector("#subscribe-email").value.trim();
    if(!email){
      subscribeMessage.textContent="Enter your email address.";
      subscribeMessage.classList.add("is-error");
      return;
    }
    const submit=subscribeForm.querySelector(".account-submit");
    submit.disabled=true;
    try{
      const data=await requestApi("/Home/Subscribe",{method:"POST",credentials:"same-origin",headers:{"Content-Type":"application/x-www-form-urlencoded;charset=UTF-8"},body:formBody({email}).toString()});
      subscribeMessage.textContent=data.message||"You are on the list.";
      subscribeMessage.classList.remove("is-error");
      subscribeForm.reset();
      closeSubscribeModal();
      showToast(data.message||"You are on the list.");
    }catch(error){
      subscribeMessage.textContent=explainAccountError(error);
      subscribeMessage.classList.add("is-error");
    }finally{submit.disabled=false;}
  });
}

updateScreenHeight();
if(reducedMotion)document.querySelector(".hero-video").pause();
setMediaSlide(0);
setOverviewSlide(0);
