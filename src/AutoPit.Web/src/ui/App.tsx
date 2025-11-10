import React, { useState } from 'react'
const base = import.meta.env.VITE_API_BASE as string
export default function App(){
  return (<div style={{fontFamily:'system-ui, sans-serif', padding:'2rem', maxWidth:1100, margin:'0 auto'}}>
    <h1>AutoPit – React UI</h1>
    <p style={{opacity:.8}}>Dealership-lite: register cars, create service requests, watch worker fulfill orders.</p>
    <div style={{display:'grid', gridTemplateColumns:'repeat(auto-fit,minmax(320px,1fr))', gap:'1rem'}}>
      <AddCarCard/><CreateServiceCard/><StatusCard/><QueueCard/>
    </div>
  </div>)
}
function Card({title, children}:{title:string, children:React.ReactNode}){return <div style={{border:'1px solid #e5e7eb', borderRadius:12, padding:16}}><h3 style={{marginTop:0}}>{title}</h3>{children}</div>}
function Field({label, children}:{label:string, children:React.ReactNode}){return <label style={{display:'block', marginTop:8}}><div style={{fontSize:12, opacity:.7}}>{label}</div>{children}</label>}
function Input(props: React.InputHTMLAttributes<HTMLInputElement>){return <input {...props} style={{padding:8, width:'100%', boxSizing:'border-box', border:'1px solid #e5e7eb', borderRadius:8}}/>}
function Button(props: React.ButtonHTMLAttributes<HTMLButtonElement> & {children:React.ReactNode}){return <button {...props} style={{padding:'8px 12px', borderRadius:8, border:'1px solid #e5e7eb', background:'#111827', color:'white'}}>{props.children}</button>}
function Pre({value}:{value:any}){return <pre style={{background:'#f9fafb', padding:12, borderRadius:8, overflow:'auto', maxHeight:220}}>{value}</pre>}
function AddCarCard(){const [vin,setVin]=useState('1HGCM82633A004352');const [make,setMake]=useState('Honda');const [model,setModel]=useState('Accord');const [year,setYear]=useState<number>(2019);const [trim,setTrim]=useState('EX');const [out,setOut]=useState('');
  async function addCar(){const res=await fetch(base+'/api/cars',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({vin,make,model,year,trim})});setOut(await res.text());}
  return <Card title="Add Car"><Field label="VIN"><Input value={vin} onChange={e=>setVin(e.target.value)}/></Field>
  <Field label="Make"><Input value={make} onChange={e=>setMake(e.target.value)}/></Field>
  <Field label="Model"><Input value={model} onChange={e=>setModel(e.target.value)}/></Field>
  <Field label="Year"><Input type="number" value={year} onChange={e=>setYear(parseInt(e.target.value||'0'))}/></Field>
  <Field label="Trim"><Input value={trim} onChange={e=>setTrim(e.target.value)}/></Field>
  <div style={{marginTop:8}}><Button onClick={addCar}>Save</Button></div><Pre value={out}/></Card>}
function CreateServiceCard(){const [vin,setVin]=useState('1HGCM82633A004352');const [concern,setConcern]=useState('Oil change & inspection');const [priority,setPriority]=useState<number>(3);const [out,setOut]=useState('');
  async function createReq(){const res=await fetch(base+'/api/service',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({vin,concern,priority})});setOut(await res.text());}
  return <Card title="Create Service Request"><Field label="VIN"><Input value={vin} onChange={e=>setVin(e.target.value)}/></Field>
  <Field label="Concern"><Input value={concern} onChange={e=>setConcern(e.target.value)}/></Field>
  <Field label="Priority 1..5"><Input type="number" value={priority} onChange={e=>setPriority(parseInt(e.target.value||'0'))}/></Field>
  <div style={{marginTop:8}}><Button onClick={createReq}>Submit</Button></div><Pre value={out}/></Card>}
function StatusCard(){const [id,setId]=useState('');const [out,setOut]=useState('');async function check(){const res=await fetch(base+'/api/service/'+id);setOut(await res.text());}
  return <Card title="Check Status"><Field label="Request Id"><Input value={id} onChange={e=>setId(e.target.value)}/></Field>
  <div style={{marginTop:8}}><Button onClick={check}>Check</Button></div><Pre value={out}/></Card>}
function QueueCard(){const [out,setOut]=useState('');async function pull(){const res=await fetch(base+'/api/service/inqueue');setOut(await res.text());}
  return <Card title="In Queue"><div><Button onClick={pull}>Refresh</Button></div><Pre value={out}/></Card>}
