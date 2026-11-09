/* NoteBoard Physics: curated local modules and bounded, data-only project state. */
(function(root,factory){const api=factory();if(typeof module==='object'&&module.exports)module.exports=api;else root.NBPhysics=api;})(typeof window!=='undefined'?window:globalThis,function(){
'use strict';
const groups={mechanics:'Cơ học',oscillation:'Dao động',waves:'Sóng',electric:'Điện học',magnetic:'Từ trường & cảm ứng',thermal:'Nhiệt học',nuclear:'Hạt nhân'};
const rows=[
 ['force-vector','ForceVectorSimulation','Tổng hợp & phân tích lực','mechanics','Kéo lực, xoay trục và quan sát các thành phần.'],
 ['inclined-plane','InclinedPlaneSimulation','Mặt phẳng nghiêng & ma sát','mechanics','Kéo thả vật trên dốc và mặt phẳng ngang.'],
 ['collision','CollisionSimulation','Va chạm & động lượng','mechanics','Thay đổi khối lượng, vận tốc và hệ số phục hồi.'],
 ['projectile','ProjectileMotionSimulation','Chuyển động ném','mechanics','Ném ngang, ném xiên, quỹ đạo và vectơ vận tốc.',1],
 ['free-fall','FreeFall3DSimulation','Rơi tự do','mechanics','Quan sát rơi tự do và các đồ thị chuyển động.',1],
 ['circular-motion','CircularMotionSimulation','Chuyển động tròn','mechanics','Góc quay, vận tốc và gia tốc hướng tâm.'],
 ['motion-graphs','MotionGraphsSimulation','Đồ thị chuyển động','mechanics','Liên hệ chuyển động với đồ thị.'],
 ['displacement-time','DisplacementTimeSimulation','Độ dịch chuyển – thời gian','mechanics','Chuyển động và đồ thị x–t.'],
 ['vt-graph','InteractiveVTGraph','Đồ thị vận tốc – thời gian','mechanics','Kéo đồ thị để thay đổi vận tốc đầu và gia tốc.'],
 ['hooke-spring','HookeSpringSimulation','Lò xo & định luật Hooke','mechanics','Độ biến dạng và lực đàn hồi.'],
 ['spring-oscillator','SpringOscillatorSimulation','Con lắc lò xo','oscillation','Kéo thả vật; thay đổi m, k và ma sát.'],
 ['simple-pendulum','SimplePendulumSimulation','Con lắc đơn','oscillation','Kéo thả con lắc, điều chỉnh chiều dài và lực cản.'],
 ['energy-oscillation','EnergyOscillationSimulation','Năng lượng dao động','oscillation','Động năng, thế năng và cơ năng.'],
 ['two-oscillations','TwoOscillationsSimulation','Độ lệch pha của hai dao động','oscillation','Hai dao động cùng tần số; thay đổi biên độ, pha và so sánh hai đồ thị.'],
 ['driven-resonance','DrivenResonanceSimulation','Dao động cưỡng bức & cộng hưởng','oscillation','Thay đổi tần số cưỡng bức và độ cản.'],
 ['wave-propagation','WavePropagationSimulation','Sự truyền sóng','waves','Theo dõi sự lan truyền dao động.'],
 ['wave-interference','WaveInterferenceSimulation','Giao thoa sóng','waves','Khoảng cách nguồn và bước sóng.'],
 ['standing-wave','StandingWaveSimulation','Sóng dừng','waves','Nút, bụng sóng và các mode dao động.'],
 ['coulomb','CoulombForceSimulation','Lực Coulomb','electric','Kéo điện tích và đổi dấu điện tích.',0,true],
 ['electric-field','ElectricFieldSimulation','Điện tích trong điện trường','electric','Điện trường, điện tích và chuyển động.',0,true],
 ['resistance-ohm','ResistanceOhmSimulation','Điện trở & định luật Ohm','electric','Đặc tuyến điện trở, đèn sợi đốt và NTC.'],
 ['circuit','CircuitSimulation','Nguồn điện & mạch kín','electric','Đóng mở công tắc, quan sát dòng điện và đèn.',1,true],
 ['emf-measurement','EMFMeasurementSimulation','Đo suất điện động','electric','Khảo sát nguồn điện qua mô hình đo.'],
 ['magnetic-field','MagneticFieldSimulation','Từ trường','magnetic','Quan sát hình dạng từ trường trong không gian.',1,true],
 ['magnetic-force','MagneticForceSimulation','Lực từ','magnetic','Thay đổi đại lượng và góc đặt trong từ trường.',1],
 ['em-induction','EMInductionSimulation','Cảm ứng điện từ','magnetic','Kéo nam châm qua cuộn dây; quan sát chiều cảm ứng.',1,true],
 ['ac-generator','AcGeneratorSimulation','Máy phát điện xoay chiều','magnetic','Khung dây quay, từ thông và suất điện động.',1],
 ['gas-law','GasLawSimulation','Các định luật chất khí','thermal','Thay đổi nhiệt độ, áp suất và thể tích.',0,true],
 ['gas-particles','GasParticleSimulation','Chuyển động phân tử khí','thermal','Quan sát mô hình hạt và va chạm.',0,true],
 ['heating-curve','HeatingCurveSimulation','Đường đun nóng','thermal','Nhiệt lượng, nhiệt độ và sự chuyển thể.'],
 ['specific-heat','SpecificHeatSimulation','Nhiệt dung riêng','thermal','Đun nóng vật và theo dõi nhiệt độ.'],
 ['phase-change','PhaseChangeSimulation','Sự chuyển thể','thermal','Quan sát các quá trình chuyển thể.'],
 ['vaporization','VaporizationSimulation','Sự hóa hơi','thermal','Khảo sát quá trình hóa hơi.'],
 ['nucleus','NucleusStructureSimulation','Cấu tạo hạt nhân','nuclear','Proton, neutron và mô hình hạt nhân.',1,true],
 ['nuclear-reaction','NuclearReactionSimulation','Phản ứng hạt nhân','nuclear','Khảo sát các dạng phản ứng hạt nhân.',0,true],
 ['radioactive-decay','RadioactiveDecaySimulation','Phóng xạ & chu kì bán rã','nuclear','Số hạt còn lại và quy luật phân rã.'],
 ['newton-inertia-3d','standalone/co-hoc/dinh-luat-1-newton-quan-tinh-3d.html','Định luật I Newton & quán tính (3D)','mechanics','Khảo sát chuyển động thẳng đều của vật khi không chịu tác dụng của ngoại lực.',1,true,'simulation',true],
 ['kinematics-3d','standalone/co-hoc/mo-phong-dong-hoc-3d.html','Mô phỏng động học 3D','mechanics','Khảo sát chuyển động nhanh dần đều, thẳng đều, chậm dần đều với đồ thị d–t, v–t, a–t.',1,true,'simulation',true],
 ['transformer-3d','standalone/dien-tu-hoc/may-bien-ap-3d.html','Máy biến áp (3D)','magnetic','Mô phỏng 3D cấu tạo lõi thép, cuộn sơ cấp, thứ cấp và nguyên lí cảm ứng điện từ.',1,true,'simulation',true],
 ['induction-cooker-3d','standalone/dien-tu-hoc/bep-tu-cam-ung-3d.html','Bếp từ (cảm ứng điện từ 3D)','magnetic','Cấu tạo 4 tầng, dòng cao tần IGBT, từ trường biến thiên và dòng Fu-cô làm nóng đáy nồi.',1,true,'simulation',true],
 ['brownian-motion-3d','standalone/nhiet-hoc/chuyen-dong-brown-3d.html','Chuyển động Brown (3D)','thermal','Quan sát chuyển động hỗn loạn không ngừng của hạt khói/bụi do va chạm phân tử khí.',1,true,'simulation',true],
 ['boyle-law-3d','standalone/nhiet-hoc/dinh-luat-boyle-3d.html','Định luật Boyle (3D)','thermal','Thí nghiệm khảo sát quá trình đẳng nhiệt của khối khí và đồ thị p–V trong không gian 3D.',1,true,'simulation',true],
 ['steam-engine-3d','standalone/nhiet-hoc/dong-co-hoi-nuoc-3d.html','Động cơ hơi nước (3D)','thermal','Mô hình 3D nguyên tắc hoạt động động cơ hơi nước: xi lanh, pít-tông và van trượt.',1,true,'simulation',true],
 ['rutherford-alpha-3d','standalone/vat-li-hat-nhan/tan-xa-hat-alpha-3d.html','Tán xạ hạt α (Rutherford 3D)','nuclear','Thí nghiệm tán xạ hạt alpha qua lá vàng mỏng của Rutherford và mô hình nguyên tử.',1,true,'simulation',true],
 ['mass-defect-he4-3d','standalone/vat-li-hat-nhan/do-hut-khoi-he4-3d.html','Độ hụt khối & liên kết He-4 (3D)','nuclear','Khối lượng hạt nhân Heli-4, độ hụt khối và năng lượng liên kết tạo thành hạt nhân.',1,true,'simulation',true],
 ['nuclear-power-plant-3d','standalone/vat-li-hat-nhan/nha-may-dien-hat-nhan-3d.html','Nhà máy điện hạt nhân (3D)','nuclear','Mô hình 3D lò phản ứng hạt nhân, thanh điều khiển, bộ trao đổi nhiệt và tua-bin.',1,true,'simulation',true]
];
const items=rows.map(([id,source,title,group,description,three,illustrative,kind,standalone])=>({id,source,title,group,description,three:!!three,illustrative:!!illustrative,kind:kind||'simulation',standalone:!!standalone}));
function data(value,depth=0,budget={n:0}){
 if(++budget.n>18000||depth>7)return undefined;
 if(value===null||typeof value==='boolean')return value;
 if(typeof value==='number')return Number.isFinite(value)&&Math.abs(value)<=1e7?value:undefined;
 if(typeof value==='string')return value.length<=2000?value:undefined;
 if(Array.isArray(value)){if(value.length>2500)return undefined;const out=value.map(v=>data(v,depth+1,budget));return out.some(v=>v===undefined)?undefined:out;}
 if(value&&typeof value==='object'&&Object.getPrototypeOf(value)===Object.prototype){const keys=Object.keys(value);if(keys.length>100)return undefined;const out={};for(const k of keys){if(!/^[\w.:-]{1,160}$/.test(k)||['__proto__','prototype','constructor'].includes(k))return undefined;const v=data(value[k],depth+1,budget);if(v===undefined)return undefined;out[k]=v;}return out;}
 return undefined;
}
function clean(input){
 // Older lesson files may contain the retired single-oscillation widget.
 if(input?.id==='oscillation')input={id:'two-oscillations',states:{},refs:{}};
 if(!input||typeof input!=='object'||!items.some(i=>i.id===input.id))return null;
 const states=data(input.states||{}),refs=data(input.refs||{});if(!states||typeof states!=='object'||Array.isArray(states)||!refs||typeof refs!=='object'||Array.isArray(refs))return null;
 const out={version:1,id:input.id,states,refs};
 return out;
}
function csvCell(value){let s=String(value);if(typeof value==='string'&&/^[=+\-@\t\r]/.test(s))s="'"+s;return '"'+s.replace(/"/g,'""')+'"';}
return {version:1,groups,items,get:id=>items.find(i=>i.id===id),clean,data,csvCell};
});
