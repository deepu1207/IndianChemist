"""Integration checks against a running Development instance (uses isolated test accounts)."""
import os
import urllib.request, urllib.error, http.cookiejar, json, uuid
base=os.environ.get('TEST_BASE_URL','http://localhost:5080')
jar=http.cookiejar.CookieJar(); client=urllib.request.build_opener(urllib.request.HTTPCookieProcessor(jar))
def req(path,method='GET',data=None,token=None,expected=200):
    h={'Content-Type':'application/json'}
    if token:h['Authorization']='Bearer '+token
    r=urllib.request.Request(base+path,data=json.dumps(data).encode() if data is not None else None,headers=h,method=method)
    try:
        with client.open(r) as response: status=response.status; raw=response.read()
    except urllib.error.HTTPError as e:status=e.code;raw=e.read()
    if path=='/api/auth/register' and status==400 and b'already registered' in raw:return {}
    assert status==expected,(path,status,expected,raw)
    try:return json.loads(raw)
    except:return raw.decode()
suffix=uuid.uuid4().hex[:8]
password='IntegrationPass123!'
email='test-'+suffix+'@example.com'
r=req('/api/auth/register','POST',{'name':'Test user','email':email,'password':password})
req('/api/auth/login','POST',{'email':email,'password':password},expected=403)
req(r['verificationUrl'].replace(base,''))
u=req('/api/auth/login','POST',{'email':email,'password':password})['accessToken']
req('/api/products',expected=401)
req('/api/products',token=u)
p={'name':'Integration product','email':'product@example.com','description':'Validation sample','price':12.5,'quantity':3,'sex':'Other','countryId':1,'stateId':1,'cityId':1}
req('/api/products','POST',p,u,403)
req('/api/products/1','PUT',p,u,403)
req('/api/products/1','DELETE',token=u,expected=403)
# The designated development admin is registered and verified through the same flow.
aemail='admin@indianchemist.local'
r=req('/api/auth/register','POST',{'name':'Administrator','email':aemail,'password':password})
if r: req(r['verificationUrl'].replace(base,''))
a=req('/api/auth/login','POST',{'email':aemail,'password':password})['accessToken']
assert len(req('/api/countries/1/states',token=a))==3
assert len(req('/api/states/1/cities',token=a))==2
for change in [{'quantity':0},{'quantity':1.5},{'price':0},{'email':'bad'},{'sex':'Invalid'},{'stateId':4},{'cityId':9},{'name':''}]:
    req('/api/products','POST',p|change,a,400)
created=req('/api/products','POST',p,a,201);pid=created['id']
assert req('/api/products/'+str(pid),token=u)['name']==p['name']
req('/api/products/'+str(pid),'PUT',p|{'quantity':8},a)
assert req('/api/products/'+str(pid),token=a)['quantity']==8
rotated=req('/api/auth/refresh','POST')['accessToken']
req('/api/products',token=rotated)
req('/api/products/'+str(pid),'DELETE',token=a,expected=204)
req('/api/products/'+str(pid),token=a,expected=404)
req('/api/auth/logout','POST')
req('/api/auth/refresh','POST',expected=401)
print('PASS: verification gate, JWT, roles, CRUD, validation, cascading lookup, refresh rotation and logout.')


