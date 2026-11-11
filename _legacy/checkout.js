"use strict";

const checkoutForm=document.querySelector("#checkout-form");
const checkoutError=document.querySelector("#checkout-error");
const checkoutSubmit=document.querySelector("#checkout-submit");
const checkoutEmail=document.querySelector("#checkout-email");
const checkoutReceipt=document.querySelector("#checkout-receipt");

async function requestCheckoutApi(url,options){
  const response=await fetch(url,options);
  const data=await response.json().catch(()=>({}));
  if(!response.ok){
    const error=new Error(data.error||"The request could not be completed.");
    error.status=response.status;
    throw error;
  }
  return data;
}

async function requireSignedInUser(){
  try{
    const data=await requestCheckoutApi("/api/me",{credentials:"same-origin"});
    if(!data.user){window.location.replace("/?checkout=login");return false;}
    checkoutEmail.textContent=data.user.email;
    return true;
  }catch{
    checkoutEmail.textContent="Could not check your account";
    checkoutError.textContent="The account service is unavailable. Return to the website and try again.";
    checkoutSubmit.disabled=true;
    return false;
  }
}

checkoutForm.addEventListener("submit",async event=>{
  event.preventDefault();
  checkoutError.textContent="";
  checkoutSubmit.disabled=true;
  checkoutSubmit.querySelector("span").textContent="Saving demo order…";
  try{
    const data=await requestCheckoutApi("/api/purchases",{
      method:"POST",
      credentials:"same-origin",
      headers:{"Content-Type":"application/json"},
      body:JSON.stringify({confirmDemoPayment:true})
    });
    document.querySelector("#receipt-id").textContent=data.purchase.id;
    checkoutForm.hidden=true;
    checkoutReceipt.hidden=false;
  }catch(error){
    if(error.status===401){window.location.replace("/?checkout=login");return;}
    checkoutError.textContent=error.message||"The demo order could not be saved.";
    checkoutSubmit.disabled=false;
    checkoutSubmit.querySelector("span").textContent="Confirm demo checkout";
  }
});

requireSignedInUser();
