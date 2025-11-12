using System;
using Alwajeih.Models;
using Alwajeih.Data.Repositories;

namespace Alwajeih.Services
{
    public class MemberService
    {
        private readonly MemberRepository _memberRepository;
        private readonly AuditRepository _auditRepository;

        public MemberService()
        {
            _memberRepository = new MemberRepository();
            _auditRepository = new AuditRepository();
        }

        public (bool Success, string Message, int MemberID) AddMember(string name, string? phone, MemberType memberType, int createdBy)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return (false, "الاسم مطلوب", 0);

                var member = new Member
                {
                    Name = name,
                    Phone = phone,
                    MemberType = memberType,
                    CreatedBy = createdBy
                };

                int memberId = _memberRepository.Add(member);

                string memberTypeText = memberType == MemberType.Regular ? "عضو أساسي" : "خلف الجمعية";
                _auditRepository.Add(new AuditLog
                {
                    UserID = createdBy,
                    Action = AuditAction.Create,
                    EntityType = EntityType.Member,
                    EntityID = memberId,
                    Details = $"إضافة عضو جديد: {name} ({memberTypeText})"
                });

                return (true, "تم إضافة العضو بنجاح", memberId);
            }
            catch (Exception ex)
            {
                return (false, $"خطأ: {ex.Message}", 0);
            }
        }

        public (bool Success, string Message) UpdateMember(Member member, int userId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(member.Name))
                    return (false, "الاسم مطلوب");

                _memberRepository.Update(member);

                _auditRepository.Add(new AuditLog
                {
                    UserID = userId,
                    Action = AuditAction.Update,
                    EntityType = EntityType.Member,
                    EntityID = member.MemberID,
                    Details = $"تحديث بيانات العضو: {member.Name}"
                });

                return (true, "تم تحديث البيانات بنجاح");
            }
            catch (Exception ex)
            {
                return (false, $"خطأ: {ex.Message}");
            }
        }

        public (bool Success, string Message) ArchiveMember(int memberId, int userId)
        {
            try
            {
                _memberRepository.Archive(memberId);

                _auditRepository.Add(new AuditLog
                {
                    UserID = userId,
                    Action = AuditAction.Archive,
                    EntityType = EntityType.Member,
                    EntityID = memberId,
                    Details = "أرشفة عضو"
                });

                return (true, "تم أرشفة العضو بنجاح");
            }
            catch (Exception ex)
            {
                return (false, $"خطأ: {ex.Message}");
            }
        }
    }
}
