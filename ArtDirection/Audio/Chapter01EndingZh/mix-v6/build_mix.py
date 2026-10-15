from pathlib import Path
import json,sys,wave,subprocess,shutil,csv
import numpy as np
P=Path(__file__).resolve().parent;VOICE=P.parent/'v5';PROJECT=P.parents[3]
sys.path.insert(0,str(PROJECT/'output/prologue-film/tools'));import imageio_ffmpeg
FF=imageio_ffmpeg.get_ffmpeg_exe();SR=44100
SOURCE=P.parent/'mix-v1/source'

def read(path):
 b=subprocess.run([FF,'-v','error','-i',str(path),'-ar',str(SR),'-ac','2','-f','f32le','-'],capture_output=True,check=True).stdout
 return np.frombuffer(b,'<f4').reshape(-1,2).astype(float)
def write(path,a):
 with wave.open(str(path),'wb') as w:w.setnchannels(2);w.setsampwidth(2);w.setframerate(SR);w.writeframes(np.round(a*32767).astype('<i2').tobytes())
def mp3(path):subprocess.run([FF,'-v','error','-y','-i',str(path),'-c:a','libmp3lame','-b:a','192k',str(path.with_suffix('.mp3'))],check=True)
def filt(a,lo,hi):
 f=np.fft.rfftfreq(len(a),1/SR);h=(1-np.exp(-(f/lo)**4))/(1+(f/hi)**8)
 return np.fft.irfft(np.fft.rfft(a,axis=0)*h[:,None],n=len(a),axis=0)
def fades(a,attack=.025,release=.15):
 a=a.copy();k=min(round(attack*SR),len(a)//3);j=min(round(release*SR),len(a)//3)
 a[:k]*=np.linspace(0,1,k)[:,None];a[-j:]*=np.linspace(1,0,j)[:,None];return a
pages=[read(VOICE/f'page{i:02}.wav') for i in range(1,6)];sizes=[len(a) for a in pages];offsets=np.cumsum([0]+sizes[:-1])/SR
# ffmpeg mono -> stereo applies equal-power channel gains, preserving perceived voice level.
voice=np.concatenate(pages);N=len(voice)
loop=read(P/'original-cabin-loop.wav')
# Preserve original timbre, transient envelope and spacing; rotate the whole loop only.
# Retain room tone while removing the old loop's three isolated fire ticks.
for t in [7.4,23.8,38.2]:
 left=round((t-.04)*SR);right=round((t+.36)*SR)
 loop[left:right]=np.linspace(loop[left-1],loop[right],right-left)
shift=6.85;loop=np.roll(loop,round(shift*SR),axis=0)
bed=np.tile(loop,(int(np.ceil(N/len(loop))),1))[:N]*2.0
bed=fades(bed,1.2,2.0)
# Gentle ducking follows voice envelope; only ambience/foley are affected.
hop=441;blocks=np.pad(voice[:,0],(0,(-N)%hop)).reshape(-1,hop);rms=np.sqrt(np.mean(blocks**2,axis=1))
activity=np.clip((rms-.004)/.018,0,1);smooth=np.convolve(activity,np.ones(25)/25,mode='same')
duck=np.repeat(1-.32*smooth,hop)[:N]
foley=np.zeros_like(voice);cues=[]
def put(kind,file,page,at,db,pan,anchor,anchor_start,anchor_end,reason):
 a=filt(read(SOURCE/file),130 if kind=='drag' else 250,4000 if kind=='drag' else 6000)
 # Remove only digital leading/trailing silence so cue placement follows audible onset.
 energy=np.sqrt(np.mean(a*a,axis=1));active=np.where(energy>max(.0005,energy.max()*.012))[0]
 if len(active):a=a[max(0,active[0]-int(.015*SR)):min(len(a),active[-1]+int(.10*SR))]
 # Tame isolated generated clicks before setting texture level, so rustle stays audible.
 ceiling=max(4*np.sqrt(np.mean(a*a)),1e-6);a=ceiling*np.tanh(a/ceiling)
 a=fades(a);gain=10**(db/20)/max(np.sqrt(np.mean(a*a)),1e-9);a*=min(gain,.105/max(abs(a).max(),1e-9))
 a*=np.sqrt(np.array([1-pan,1+pan]));t=offsets[page-1]+at;j=round(t*SR);end=min(N,j+len(a));foley[j:end]+=a[:end-j]
 cues.append(dict(kind=kind,source=file,page=page,page_start=at,global_start=round(t,3),duration=round(len(a)/SR,3),anchor=anchor,anchor_page_start=anchor_start,anchor_page_end=anchor_end,anchor_global_start=round(offsets[page-1]+anchor_start,3),reason=reason,rms_target_dbfs=db))
put('drawer','drawer-1.mp3',2,3.78,-33,-.22,'旧抽屉',3.72,4.28,'在“抽屉”前约0.12秒起声，拉开声自然延续到找到纸。')
put('paper','paper-2.mp3',2,22.55,-35,-.12,'纸抽了出来',22.46,23.44,'纸张抽出声贴近“抽”字，动作收尾留在句后。')
put('paper','paper-1.mp3',3,1.95,-35,-.08,'那是一张地图',.45,1.76,'地图揭示后的留白展开，避免在首句起声时抢旁白。')
put('paper','paper-2.mp3',3,9.16,-36,.12,'轻轻铺平',8.28,9.16,'在“铺平”收尾处放轻短纸声。')
put('drag','drag-2.mp3',4,5.13,-32,.15,'尸体拖到屋外',4.84,6.12,'第一下重物摩擦在“拖”前0.13秒开始。')
put('drag','drag-3.mp3',4,6.18,-36,.22,'拖到屋外',5.26,6.12,'第二下更轻的拖动余声，随动作离开室内而减弱。')
original=read(P/'original-cabin-loop.wav')
fire_focus=np.zeros_like(voice)
for at,source_time,gain in [(13.25,7.4,4.0),(13.43,23.8,4.0),(14.08,38.2,4.0)]:
 pre=.04;post=.20
 tick=original[round((source_time-pre)*SR):round((source_time+post)*SR)].copy()
 tick=fades(tick,.008,.04)*gain
 j=round((at-pre)*SR);fire_focus[j:j+len(tick)]+=tick
 cues.append(dict(kind='fire_focus',source='original-cabin-loop.wav',page=1,page_start=at,global_start=at,duration=len(tick)/SR,anchor='炉火的噼啪声',anchor_page_start=13.04,anchor_page_end=14.24,anchor_global_start=13.04,reason='一次性啪啪—停顿—啪：13.25/13.43秒，停顿后14.08秒；比mix-v5提前1秒并删除最后一下。'))
sfx=bed+foley*duck[:,None]+fire_focus;mixed=voice+sfx
assert abs(mixed).max()<.98
write(P/'chapter01-ending-mix.wav',mixed);mp3(P/'chapter01-ending-mix.wav')
write(P/'fire-preview.wav',mixed[round(11*SR):round(18*SR)]);mp3(P/'fire-preview.wav')
write(P/'fire-pattern-only.wav',fire_focus[round(12.95*SR):round(14.9*SR)])
write(P/'ambience-and-foley-stem.wav',sfx);write(P/'narration-stem.wav',voice)
start=0
for i,n in enumerate(sizes,1):
 write(P/f'page{i:02}-mix.wav',mixed[start:start+n]);mp3(P/f'page{i:02}-mix.wav');start+=n
 for ext in ['srt','-timings.json']:
  src=VOICE/(f'page{i:02}.'+ext if ext=='srt' else f'page{i:02}'+ext)
  shutil.copy2(src,P/src.name)
shutil.copy2(VOICE/'chapter01-ending-full.srt',P/'chapter01-ending-mix.srt')
(P/'cue-sheet.json').write_text(json.dumps(dict(page_offsets=offsets.tolist(),cues=cues,alignment_note='来源为 v5 独立逐字转写。时间精度显示到毫秒用于复现，并非毫秒级人工对齐保证；弱音起止仍有约数百毫秒识别误差。'),ensure_ascii=False,indent=2))
with (P/'cue-sheet.csv').open('w',newline='') as f:
 w=csv.writer(f);w.writerow(['声音','页','页内起点秒','全片起点秒','时长秒','旁白锚点','锚点起点秒','安排理由'])
 for c in cues:w.writerow([c['kind'],c['page'],c['page_start'],c['global_start'],c['duration'],c['anchor'],c['anchor_page_start'],c['reason']])
report=dict(duration=N/SR,peak_dbfs=float(20*np.log10(abs(mixed).max())),voice_samples_unchanged=True,continuous_bed_across_pages=True,voice_rms_dbfs=float(20*np.log10(np.sqrt(np.mean(voice*voice)))),sfx_rms_dbfs=float(20*np.log10(np.sqrt(np.mean(sfx*sfx)))),page_offsets=offsets.tolist(),unity_modified=False)
(P/'validation.json').write_text(json.dumps(report,indent=2));print(json.dumps(report,indent=2))
