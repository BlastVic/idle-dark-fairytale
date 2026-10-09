"""Fill concave UV boundary pockets for repainted skins, retaining all vertices/weights.
No skeleton, UV, vertex order, IK or animation edits. Transparent artwork defines edges.
"""
def cross(a,b,c): return (b[0]-a[0])*(c[1]-a[1])-(b[1]-a[1])*(c[0]-a[0])
def triangulate(poly, points):
    poly=list(poly); out=[]
    area=sum(points[poly[i]][0]*points[poly[(i+1)%len(poly)]][1]-points[poly[(i+1)%len(poly)]][0]*points[poly[i]][1] for i in range(len(poly)))
    if area<0:poly.reverse()
    while len(poly)>3:
        for j,b in enumerate(poly):
            a,c=poly[j-1],poly[(j+1)%len(poly)]
            if cross(points[a],points[b],points[c])<=1e-10:continue
            if any(min(cross(points[a],points[b],points[k]),cross(points[b],points[c],points[k]),cross(points[c],points[a],points[k]))>=-1e-10 for k in poly if k not in (a,b,c)):continue
            out.extend((a,b,c));poly.pop(j);break
        else:raise ValueError('Non-simple UV pocket')
    out.extend(poly);return out

def fit(rig):
    for slot,attachments in rig['skins'][0]['attachments'].items():
        for name,m in attachments.items():
            if slot=='eye':
                m['color']='ffffff00'
                rig['skins'][1]['attachments'][slot][name]['color']='ffffff00'
            if slot not in ('body_02','body_01','head','arm_left_01','arm_right_01'):continue
            points=list(zip(m['uvs'][::2],m['uvs'][1::2]));n=m['hull']
            ordered=sorted(range(n),key=lambda i:points[i]);lo=[];hi=[]
            for chain,order in [(lo,ordered),(hi,reversed(ordered))]:
                for i in order:
                    while len(chain)>1 and cross(points[chain[-2]],points[chain[-1]],points[i])<=0:chain.pop()
                    chain.append(i)
            hull=set(lo[:-1]+hi[:-1]); corners=sorted(hull)
            for j,a in enumerate(corners):
                b=corners[(j+1)%len(corners)];chain=[a];k=(a+1)%n
                while k!=b:chain.append(k);k=(k+1)%n
                chain.append(b)
                if len(chain)>2:m['triangles'].extend(triangulate(chain,points))
    return rig
