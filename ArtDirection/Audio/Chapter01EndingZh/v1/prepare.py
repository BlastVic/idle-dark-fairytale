from pathlib import Path
import sys,json,subprocess,re,wave,hashlib
ROOT=Path(__file__).resolve().parent
PROJECT=ROOT.parents[3]
sys.path.insert(0,str(PROJECT/'output/prologue-film/tools'))
import imageio_ffmpeg
FF=imageio_ffmpeg.get_ffmpeg_exe()
IDS=['bztKB7s5rat9JBy2mZNf','jbMTrjhmBBmDsKd1yHRj','IeaeOrgpAmSxCdwF1XPN','xHlfbt3MXTz3t7ftcqG5','fsxeylTlB64LRoG3rqKS']
def run(args):return subprocess.run([FF,'-hide_banner','-y']+args,check=True,capture_output=True,text=True)
def stamp(t):
 ms=round(t*1000);return f'{ms//3600000:02}:{ms//60000%60:02}:{ms//1000%60:02},{ms%1000:03}'
reports=[];allsubs=[];offset=0
for i,ident in enumerate(IDS,1):
 stem=f'page{i:02}';src=ROOT/'raw'/f'{ident}.mp3';dst=ROOT/f'{stem}.wav'
 raw=ROOT/'raw'/f'{stem}-original.wav'
 run(['-i',str(src),'-ar','44100','-ac','1','-c:a','pcm_s16le',str(raw)])
 stats=run(['-i',str(raw),'-af','loudnorm=I=-20:TP=-3:LRA=11:print_format=json','-f','null','-']).stderr
 m=json.loads(stats[stats.rfind('{'):stats.rfind('}')+1])
 filt='loudnorm=I=-20:TP=-3:LRA=11:linear=true:'+':'.join(f'{k}={m[v]}' for k,v in [('measured_I','input_i'),('measured_TP','input_tp'),('measured_LRA','input_lra'),('measured_thresh','input_thresh'),('offset','target_offset')])
 filt+=',adelay=350,apad=pad_dur=0.8'
 run(['-i',str(raw),'-af',filt,'-ar','44100','-ac','1','-c:a','pcm_s16le',str(dst)])
 run(['-i',str(dst),'-c:a','libmp3lame','-b:a','192k',str(ROOT/f'{stem}.mp3')])
 with wave.open(str(raw)) as w:rawlen=w.getnframes()/w.getframerate()
 with wave.open(str(dst)) as w:duration=w.getnframes()/w.getframerate()
 a=json.loads((ROOT/f'{stem}-alignment-raw.json').read_text());chars=[];tag=False
 for word in a['words']:
  for c in word['text']:
   if c=='[':tag=True
   elif c==']':tag=False
   elif not tag and not c.isspace():chars.append((c,word['start'],word['end']))
 full=''.join(c[0] for c in chars);sentences=(ROOT/f'{stem}-script.txt').read_text().splitlines();cursor=0;rows=[]
 for line in sentences:
  if not line:continue
  pos=full.find(line,cursor);assert pos>=0,(stem,line,full)
  end=pos+len(line);start=max(0,chars[pos][1])+.35;stop=min(rawlen,chars[end-1][2])+.35
  rows.append(dict(text=line,start=round(start,3),end=round(stop,3)))
  allsubs.append(dict(text=line,start=start+offset,end=stop+offset,page=i));cursor=end
 (ROOT/f'{stem}.srt').write_text('\n\n'.join(f'{j}\n{stamp(x["start"])} --> {stamp(x["end"])}\n{x["text"]}' for j,x in enumerate(rows,1))+'\n')
 (ROOT/f'{stem}-timings.json').write_text(json.dumps(dict(page=i,duration=duration,leading_padding=.35,trailing_padding=.8,alignment_note='自动时间对齐，供应商结果包含表演标记，已过滤；不等于独立听审。边界误差未实测，接入前逐句人工核对。',sentences=rows),ensure_ascii=False,indent=2))
 reports.append(dict(page=i,generation_id=ident,raw_duration=rawlen,delivered_duration=duration,source_sha256=hashlib.sha256(src.read_bytes()).hexdigest(),normalization=m))
 offset+=duration
with wave.open(str(ROOT/'chapter01-ending-full.wav'),'wb') as out:
 out.setnchannels(1);out.setsampwidth(2);out.setframerate(44100)
 for i in range(1,6):
  with wave.open(str(ROOT/f'page{i:02}.wav')) as inp:out.writeframes(inp.readframes(inp.getnframes()))
run(['-i',str(ROOT/'chapter01-ending-full.wav'),'-c:a','libmp3lame','-b:a','192k',str(ROOT/'chapter01-ending-full.mp3')])
(ROOT/'chapter01-ending-full.srt').write_text('\n\n'.join(f'{j}\n{stamp(x["start"])} --> {stamp(x["end"])}\n{x["text"]}' for j,x in enumerate(allsubs,1))+'\n')
(ROOT/'processing-report.json').write_text(json.dumps(reports,ensure_ascii=False,indent=2))
print(json.dumps([{'page':r['page'],'duration':r['delivered_duration']} for r in reports]))
