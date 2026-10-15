from pathlib import Path
import json,sys,re,difflib
ROOT=Path(__file__).resolve().parent;sys.path.insert(0,str(ROOT.parents[3]/'output/chapter01-ending-tools'))
from opencc import OpenCC
cc=OpenCC('t2s')
def clean(t):return ''.join(c for c in cc.convert(t) if '\u4e00'<=c<='\u9fff')
def stamp(t):
 n=round(t*1000);return f'{n//3600000:02}:{n//60000%60:02}:{n//1000%60:02},{n%1000:03}'
fullrows=[];offset=0;checks=[]
for i in range(1,6):
 stem=f'page{i:02}';lines=(ROOT/f'{stem}-script.txt').read_text().strip().splitlines();expected=''.join(clean(x) for x in lines)
 data=json.loads((ROOT/f'{stem}-independent-asr.json').read_text());actual='';timings=[]
 for seg in data:
  for w in seg['words']:
   t=clean(w['text']);actual+=t
   for n,c in enumerate(t):timings.append((w['start']+(w['end']-w['start'])*n/max(1,len(t)),w['start']+(w['end']-w['start'])*(n+1)/max(1,len(t))))
 mapping={};diff=[]
 for tag,a,b,c,d in difflib.SequenceMatcher(None,expected,actual,autojunk=False).get_opcodes():
  if tag!='equal':diff.append(dict(type=tag,script=expected[a:b],recognized=actual[c:d]))
  if d>c:
   for k in range(a,b):mapping[k]=min(d-1,c+int((k-a)*(d-c)/max(1,b-a)))
 meta=json.loads((ROOT/f'{stem}-timings.json').read_text());rows=[];pos=0
 for line in lines:
  length=len(clean(line));indices=[mapping[k] for k in range(pos,pos+length) if k in mapping]
  assert indices
  start=max(.35,timings[min(indices)][0]);end=min(meta['duration']-.8,timings[max(indices)][1]);pos+=length
  rows.append(dict(text=line,start=round(start,3),end=round(end,3)))
  fullrows.append(dict(text=line,start=start+offset,end=end+offset))
 meta['alignment_note']='独立 faster-whisper small 自动转写，与批准原文字符对齐。字幕保留批准原文。句边界未经人工逐帧校准，建议预留约 ±0.5 秒核对窗口；个别停顿或弱音可能更大，该窗口不是实测误差保证。'
 meta['sentences']=rows;(ROOT/f'{stem}-timings.json').write_text(json.dumps(meta,ensure_ascii=False,indent=2))
 (ROOT/f'{stem}.srt').write_text('\n\n'.join(f'{j}\n{stamp(r["start"])} --> {stamp(r["end"])}\n{r["text"]}' for j,r in enumerate(rows,1))+'\n')
 checks.append(dict(page=i,recognized_text=actual,differences=diff,script_characters=len(expected),recognized_characters=len(actual)))
 offset+=meta['duration']
(ROOT/'chapter01-ending-full.srt').write_text('\n\n'.join(f'{j}\n{stamp(r["start"])} --> {stamp(r["end"])}\n{r["text"]}' for j,r in enumerate(fullrows,1))+'\n')
(ROOT/'transcription-comparison.json').write_text(json.dumps(checks,ensure_ascii=False,indent=2))
print(json.dumps(checks,ensure_ascii=False))
